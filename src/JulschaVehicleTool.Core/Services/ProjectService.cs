using System.IO.Compression;
using System.Text;
using System.Text.Json;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Serialization;
using JulschaVehicleTool.Core.Services.Import;

namespace JulschaVehicleTool.Core.Services;

public interface IProjectService
{
    Project CreateNew(string folderPath);
    Project Open(string folderPath);
    void Save(Project project);

    /// <summary>
    /// Turns DPAPI encryption of the project on or off and converts the project file and all
    /// vehicle files accordingly.
    /// </summary>
    void SetLocalEncryption(Project project, bool encrypt,
        IProgress<(int current, int total, string message)>? progress = null);

    /// <summary>Deletes files in vehicles/ that no vehicle references. Returns the number of deleted files.</summary>
    int DeleteUnreferencedFiles(Project project);
    void SaveAs(Project project, string newFolderPath);

    void ImportVehicleFiles(Project project, Vehicle vehicle, string[] sourcePaths);
    void ImportVehicleFolder(Project project, Resource resource, string folderPath,
        IProgress<(int current, int total, string message)>? progress = null);

    /// <summary>
    /// Imports the confirmed vehicles of a scan into the resource: copies (and encrypts) all
    /// stream files, applies the meta entries and merges siren groups into the project pool.
    /// </summary>
    ImportResult ImportScan(Project project, Resource resource, ImportScan scan,
        IProgress<(int current, int total, string message)>? progress = null);

    /// <summary>
    /// First half of <see cref="ImportScan"/>: copies files and builds the vehicles without touching
    /// the project's collections. Safe to run on a background thread.
    /// </summary>
    ImportResult PrepareImport(Project project, ImportScan scan,
        IProgress<(int current, int total, string message)>? progress = null);

    /// <summary>Second half of <see cref="ImportScan"/>: adds the prepared vehicles and sirens (UI thread).</summary>
    void ApplyImport(Project project, Resource resource, ImportResult result);

    byte[] DecryptVehicleFile(Project project, Vehicle vehicle, string relativePath);

    void ExportProjectZip(Project project, string zipPath, string password,
        IProgress<(int current, int total, string message)>? progress = null);
    Project ImportProjectZip(string zipPath, string password, string targetFolderPath,
        IProgress<(int current, int total, string message)>? progress = null);

    void RenameVehicle(Project project, Vehicle vehicle, string oldName, string newName, bool updateMetaNames = true);
    Vehicle CloneVehicle(Project project, Vehicle vehicle, string newName);

    List<string> LoadRecentProjects();
    void AddToRecentProjects(string folderPath);
}

public class ProjectService : IProjectService
{
    private readonly IEncryptionService _encryption;
    private const string ProjectFileName = "project.julveh";
    private const string VehiclesFolder = "vehicles";
    private const string RecentProjectsFileName = "recent_projects.json";
    private const int MaxRecentProjects = 10;


    /// <summary>
    /// Files written by a running <see cref="PrepareImport"/> that are not referenced by the project yet.
    /// <see cref="DeleteUnreferencedFiles"/> must not touch them (e.g. auto-save during an import).
    /// </summary>
    private readonly HashSet<string> _pendingFiles = new(StringComparer.OrdinalIgnoreCase);

    public ProjectService(IEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public Project CreateNew(string folderPath)
    {
        Directory.CreateDirectory(folderPath);
        Directory.CreateDirectory(Path.Combine(folderPath, VehiclesFolder));

        var project = new Project
        {
            Name = Path.GetFileName(folderPath),
            FolderPath = folderPath,
            IsDirty = false,
        };

        SaveProjectFile(project);
        return project;
    }

    public Project Open(string folderPath)
    {
        var projectFilePath = Path.Combine(folderPath, ProjectFileName);
        if (!File.Exists(projectFilePath))
            throw new FileNotFoundException($"Project file not found: {projectFilePath}");

        Project project;
        bool wasEncrypted;
        bool fromBackup = false;
        try
        {
            (project, wasEncrypted) = ReadProjectFile(projectFilePath);
        }
        catch (Exception ex) when (File.Exists(projectFilePath + BackupSuffix))
        {
            // Damaged project file (e.g. crash while writing): fall back to the last good version
            try
            {
                (project, wasEncrypted) = ReadProjectFile(projectFilePath + BackupSuffix);
                fromBackup = true;
            }
            catch
            {
                throw new InvalidOperationException($"Project file and its backup could not be read: {ex.Message}", ex);
            }
        }

        // Projects from before 1.1 have no EncryptLocal flag but are DPAPI-encrypted: keep them encrypted
        if (wasEncrypted) project.EncryptLocal = true;

        project.FolderPath = folderPath;
        project.LoadedFromBackup = fromBackup;
        project.IsDirty = false;

        // Migrate old per-vehicle CarCols to the project-level pool (one-time, backward compat)
        MigrateVehicleCarColsToProject(project);
        if (fromBackup) project.IsDirty = true;

        return project;
    }

    private (Project project, bool wasEncrypted) ReadProjectFile(string path)
    {
        var raw = File.ReadAllBytes(path);
        var wasEncrypted = IsDpapiBlob(raw);
        var json = Encoding.UTF8.GetString(wasEncrypted ? _encryption.DecryptLocal(raw) : raw);
        var project = JsonSerializer.Deserialize<Project>(json, ProjectJsonOptions.Default)
                      ?? throw new InvalidOperationException("Failed to deserialize project file.");
        return (project, wasEncrypted);
    }

    // --- Local storage encoding ---

    private const string BackupSuffix = ".bak";

    // DPAPI blobs start with dwVersion = 1 followed by the provider GUID df9d8cd0-1501-11d1-8c7a-00c04fc297eb.
    // GTA resources start with "RSC7" and JSON with '{', so the formats can't be confused.
    private static readonly byte[] DpapiHeader =
        [0x01, 0x00, 0x00, 0x00, 0xD0, 0x8C, 0x9D, 0xDF, 0x01, 0x15, 0xD1, 0x11, 0x8C, 0x7A, 0x00, 0xC0, 0x4F, 0xC2, 0x97, 0xEB];

    public static bool IsDpapiBlob(byte[] data) =>
        data.Length > DpapiHeader.Length && data.AsSpan(0, DpapiHeader.Length).SequenceEqual(DpapiHeader);

    /// <summary>Reads a stored file and decrypts it if it is DPAPI-encrypted (works for mixed projects).</summary>
    private byte[] ReadStored(string path)
    {
        var raw = File.ReadAllBytes(path);
        return IsDpapiBlob(raw) ? _encryption.DecryptLocal(raw) : raw;
    }

    private byte[] EncodeStored(Project project, byte[] plain) =>
        project.EncryptLocal ? _encryption.EncryptLocal(plain) : plain;

    /// <summary>Writes via a temp file and rename so a crash never leaves a half-written file.</summary>
    private static void WriteAtomic(string path, byte[] data, string? backupPath = null)
    {
        var tmp = path + ".tmp";
        File.WriteAllBytes(tmp, data);
        if (File.Exists(path))
        {
            if (backupPath != null) File.Replace(tmp, path, backupPath, ignoreMetadataErrors: true);
            else File.Move(tmp, path, overwrite: true);
        }
        else
        {
            File.Move(tmp, path);
        }
    }

    public void SetLocalEncryption(Project project, bool encrypt,
        IProgress<(int current, int total, string message)>? progress = null)
    {
        if (string.IsNullOrEmpty(project.FolderPath))
            throw new InvalidOperationException("Project has no folder path.");

        var vehiclesDir = Path.Combine(project.FolderPath, VehiclesFolder);
        var files = Directory.Exists(vehiclesDir)
            ? Directory.GetFiles(vehiclesDir, "*", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)).ToArray()
            : [];

        project.EncryptLocal = encrypt;
        int current = 0;
        foreach (var file in files)
        {
            progress?.Report((++current, files.Length + 1, $"{(encrypt ? "Encrypting" : "Decrypting")} {Path.GetFileName(file)}..."));
            var raw = File.ReadAllBytes(file);
            if (IsDpapiBlob(raw) == encrypt) continue; // already in the target format
            WriteAtomic(file, EncodeStored(project, IsDpapiBlob(raw) ? _encryption.DecryptLocal(raw) : raw));
        }

        progress?.Report((files.Length + 1, files.Length + 1, "Saving project file..."));
        SaveProjectFile(project);
        project.IsDirty = false;

        // The backup still has the old format; replace it so it matches the setting
        var backup = Path.Combine(project.FolderPath, ProjectFileName + BackupSuffix);
        if (File.Exists(backup))
            File.Copy(Path.Combine(project.FolderPath, ProjectFileName), backup, overwrite: true);
    }

    private static void MigrateVehicleCarColsToProject(Project project)
    {
        var existingIds = new HashSet<int>(project.CarCols.SirenSettings.Select(s => s.Id));
        bool migrated = false;

        foreach (var resource in project.Resources)
        {
            foreach (var vehicle in resource.Vehicles)
            {
                if (vehicle.CarCols == null) continue;

                foreach (var siren in vehicle.CarCols.SirenSettings)
                {
                    if (existingIds.Add(siren.Id))
                        project.CarCols.SirenSettings.Add(siren);
                }

                vehicle.CarCols = null;
                migrated = true;
            }
        }

        // If migration happened, project needs to be re-saved
        if (migrated)
            project.IsDirty = true;
    }

    public void Save(Project project)
    {
        if (string.IsNullOrEmpty(project.FolderPath))
            throw new InvalidOperationException("Project has no folder path. Use SaveAs instead.");

        SaveProjectFile(project);
        project.IsDirty = false;
        DeleteUnreferencedFiles(project);
    }

    /// <summary>
    /// Full paths of all stream files referenced by any vehicle of the project.
    /// </summary>
    private static HashSet<string> ReferencedFiles(Project project)
    {
        var vehiclesDir = Path.Combine(project.FolderPath!, VehiclesFolder);
        return project.Resources.SelectMany(r => r.Vehicles).SelectMany(v => v.AllStreamFiles)
            .Select(rel => Path.GetFullPath(Path.Combine(vehiclesDir, rel)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Removes files in vehicles/ that no vehicle references any more (deleted vehicles/resources,
    /// replaced files) and empty folders. Runs after the project file was written, so the files
    /// on disk always match the saved project.
    /// </summary>
    public int DeleteUnreferencedFiles(Project project)
    {
        if (string.IsNullOrEmpty(project.FolderPath)) return 0;
        var vehiclesDir = Path.Combine(project.FolderPath, VehiclesFolder);
        if (!Directory.Exists(vehiclesDir)) return 0;

        var referenced = ReferencedFiles(project);
        int deleted = 0;

        foreach (var file in Directory.EnumerateFiles(vehiclesDir, "*", SearchOption.AllDirectories).ToList())
        {
            var full = Path.GetFullPath(file);
            if (referenced.Contains(full)) continue;
            lock (_pendingFiles)
                if (_pendingFiles.Contains(full)) continue;
            try { File.Delete(file); deleted++; } catch { /* locked: try again next save */ }
        }

        foreach (var dir in Directory.EnumerateDirectories(vehiclesDir, "*", SearchOption.AllDirectories)
                     .OrderByDescending(d => d.Length).ToList())
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any()) Directory.Delete(dir);
            }
            catch { /* ignore */ }
        }

        return deleted;
    }

    public void SaveAs(Project project, string newFolderPath)
    {
        // Copy vehicles folder if different location
        if (project.FolderPath != null && project.FolderPath != newFolderPath)
        {
            var srcVehicles = Path.Combine(project.FolderPath, VehiclesFolder);
            var dstVehicles = Path.Combine(newFolderPath, VehiclesFolder);

            if (Directory.Exists(srcVehicles))
            {
                CopyDirectory(srcVehicles, dstVehicles);
            }
        }

        Directory.CreateDirectory(newFolderPath);
        Directory.CreateDirectory(Path.Combine(newFolderPath, VehiclesFolder));

        project.FolderPath = newFolderPath;
        SaveProjectFile(project);
        project.IsDirty = false;
    }

    public void ImportVehicleFiles(Project project, Vehicle vehicle, string[] sourcePaths)
    {
        if (string.IsNullOrEmpty(project.FolderPath))
            throw new InvalidOperationException("Project must be saved before importing files.");

        var vehicleDir = Path.Combine(project.FolderPath, VehiclesFolder, vehicle.Name);
        Directory.CreateDirectory(vehicleDir);

        foreach (var sourcePath in sourcePaths)
        {
            var fileName = Path.GetFileName(sourcePath);
            var destPath = Path.Combine(vehicleDir, fileName);

            // Read, (optionally) encrypt, and write
            File.WriteAllBytes(destPath, EncodeStored(project, File.ReadAllBytes(sourcePath)));

            // Assign to the correct vehicle property based on file type
            var relativePath = Path.Combine(vehicle.Name, fileName);
            AssignFileToVehicle(vehicle, fileName, relativePath);
        }
    }

    public void ImportVehicleFolder(Project project, Resource resource, string folderPath,
        IProgress<(int current, int total, string message)>? progress = null)
    {
        var scan = new VehicleImportScanner(new MetaXmlService()).Scan(folderPath);
        ImportScan(project, resource, scan, progress);
    }

    public ImportResult ImportScan(Project project, Resource resource, ImportScan scan,
        IProgress<(int current, int total, string message)>? progress = null)
    {
        var result = PrepareImport(project, scan, progress);
        ApplyImport(project, resource, result);
        return result;
    }

    public ImportResult PrepareImport(Project project, ImportScan scan,
        IProgress<(int current, int total, string message)>? progress = null)
    {
        if (string.IsNullOrEmpty(project.FolderPath))
            throw new InvalidOperationException("Project must be saved before importing files.");

        var result = new ImportResult();
        var existingNames = new HashSet<string>(
            project.Resources.SelectMany(r => r.Vehicles).Select(v => v.Name), StringComparer.OrdinalIgnoreCase);

        var selected = scan.Vehicles.Where(v => v.Include).ToList();
        var current = 0;

        try
        {
            foreach (var candidate in selected)
            {
                progress?.Report((++current, selected.Count, $"Importing {candidate.Name}..."));

                var name = string.IsNullOrWhiteSpace(candidate.ImportAs) ? candidate.Name : candidate.ImportAs.Trim();
                if (!VehicleNaming.IsValidName(name, out var nameError))
                {
                    result.Warnings.Add($"Skipped '{candidate.Name}': {nameError}");
                    continue;
                }
                if (!existingNames.Add(name))
                {
                    result.Warnings.Add($"Skipped '{candidate.Name}': a vehicle named '{name}' already exists in the project.");
                    continue;
                }

                var vehicle = VehicleDefaults.CreateDefault(candidate.Name);
                vehicle.CarCols = null; // sirens live in the project pool
                if (candidate.VehicleMeta != null) vehicle.VehicleMeta = candidate.VehicleMeta;
                if (candidate.Handling != null) vehicle.Handling = candidate.Handling;
                if (candidate.CarVariation != null) vehicle.CarVariation = candidate.CarVariation;
                if (vehicle.VehicleMeta != null && string.IsNullOrEmpty(vehicle.VehicleMeta.DisplayName))
                    vehicle.VehicleMeta.DisplayName = vehicle.VehicleMeta.GameName;

                // Importing under a different name: rename files (old.yft → new.yft, old_bumf.yft → new_bumf.yft)
                // and all meta names that refer to the original name.
                if (name != candidate.Name)
                {
                    vehicle.Name = name;
                    VehicleNaming.ApplyNameToMetas(vehicle, candidate.Name, name);
                }

                string? Store(string? source)
                {
                    if (source == null) return null;
                    var fileName = VehicleNaming.RenameFileName(Path.GetFileName(source), candidate.Name, name);
                    var full = Path.GetFullPath(Path.Combine(project.FolderPath!, VehiclesFolder, name, fileName));
                    lock (_pendingFiles) _pendingFiles.Add(full);
                    result.StoredFiles.Add(full);
                    return StoreFile(project, name, source, fileName);
                }

                vehicle.YftRelativePath = Store(candidate.Model);
                vehicle.YftHiRelativePath = Store(candidate.ModelHi);
                vehicle.YtdRelativePath = Store(candidate.Texture);
                vehicle.YtdHiRelativePath = Store(candidate.TextureHi);
                foreach (var extra in candidate.ModParts.Concat(candidate.ExtraTextures))
                    vehicle.ExtraFiles.Add(Store(extra)!);

                result.ImportedVehicles.Add(vehicle);
            }

            // Siren groups: only those referenced by imported vehicles
            var referenced = result.ImportedVehicles
                .Select(v => v.CarVariation?.SirenSettings ?? 0).Where(id => id != 0).ToHashSet();
            var reserved = SirenIds.ParseList(project.ReservedSirenIds);
            var taken = SirenIds.Used(project);
            foreach (var siren in scan.CarCols.SirenSettings.Where(s => referenced.Contains(s.Id)))
            {
                var existing = project.CarCols.SirenSettings.FirstOrDefault(s => s.Id == siren.Id);
                if (existing != null && SameSiren(existing, siren))
                    continue; // identical group already in the pool

                bool conflict = existing != null || reserved.Contains(siren.Id) || !SirenIds.IsValid(siren.Id);
                if (conflict)
                {
                    var newId = SirenIds.NextFree(taken);
                    if (newId == null)
                    {
                        result.Warnings.Add($"Siren setting {siren.Id} ('{siren.Name}') conflicts and no free ID (1–255) is left — not imported.");
                        continue;
                    }

                    var reason = existing != null ? "already used in the project"
                               : reserved.Contains(siren.Id) ? "reserved on the server"
                               : "outside 1–255";
                    result.Warnings.Add($"Siren setting {siren.Id} ('{siren.Name}') is {reason} — renumbered to {newId}.");

                    foreach (var v in result.ImportedVehicles.Where(v => v.CarVariation?.SirenSettings == siren.Id))
                        v.CarVariation!.SirenSettings = newId.Value;
                    siren.Id = newId.Value;
                }

                taken.Add(siren.Id);
                result.SirensToAdd.Add(siren);
            }

            // Mod kits (referenced by kitName) and light settings (referenced by id)
            var kitNames = result.ImportedVehicles.SelectMany(v => v.CarVariation?.Kits ?? [])
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var kit in scan.CarCols.Kits.Where(k => kitNames.Contains(k.Name)))
            {
                var sameName = project.CarCols.Kits.FirstOrDefault(k => k.Name.Equals(kit.Name, StringComparison.OrdinalIgnoreCase));
                var sameId = project.CarCols.Kits.FirstOrDefault(k => k.Id == kit.Id);
                if (sameName != null)
                {
                    if (!SameXml(sameName.Xml, kit.Xml))
                        result.Warnings.Add($"Mod kit '{kit.Name}' already exists in the project with different content — kept the existing one.");
                    continue;
                }
                if (sameId != null)
                {
                    result.Warnings.Add($"Mod kit '{kit.Name}' uses ID {kit.Id}, which is already used by '{sameId.Name}' — not imported. Change the kit ID in the carcols.meta and import again.");
                    continue;
                }
                result.KitsToAdd.Add(kit);
            }

            var lightIds = result.ImportedVehicles.Select(v => v.CarVariation?.LightSettings ?? -1).ToHashSet();
            foreach (var light in scan.CarCols.Lights.Where(l => lightIds.Contains(l.Id)))
            {
                var existing = project.CarCols.Lights.FirstOrDefault(l => l.Id == light.Id);
                if (existing == null)
                    result.LightsToAdd.Add(light);
                else if (!SameXml(existing.Xml, light.Xml))
                    result.Warnings.Add($"Light setting {light.Id} already exists in the project with different content — kept the existing one.");
            }

            // Pass-through metas (vehiclelayouts.meta, ...) of the pack
            if (result.ImportedVehicles.Count > 0)
            {
                foreach (var meta in scan.MetaFiles.Where(m => m.Kind == MetaKind.Passthrough))
                {
                    result.ExtraMetasToAdd.Add(new PassthroughMeta
                    {
                        FileName = Path.GetFileName(meta.Path),
                        DataFileType = meta.DataFileType!,
                        Content = File.ReadAllText(meta.Path),
                    });
                }
            }
        }
        catch
        {
            ReleasePending(result);
            throw;
        }

        return result;
    }

    private void ReleasePending(ImportResult result)
    {
        lock (_pendingFiles)
            foreach (var f in result.StoredFiles)
                _pendingFiles.Remove(f);
    }

    public void ApplyImport(Project project, Resource resource, ImportResult result)
    {
        ReleasePending(result);
        foreach (var vehicle in result.ImportedVehicles)
            resource.Vehicles.Add(vehicle);

        foreach (var siren in result.SirensToAdd)
        {
            if (project.CarCols.SirenSettings.Any(s => s.Id == siren.Id)) continue;
            project.CarCols.SirenSettings.Add(siren);
            result.ImportedSirenIds.Add(siren.Id);
        }

        foreach (var kit in result.KitsToAdd)
            if (project.CarCols.Kits.All(k => k.Id != kit.Id && !k.Name.Equals(kit.Name, StringComparison.OrdinalIgnoreCase)))
                project.CarCols.Kits.Add(kit);

        foreach (var light in result.LightsToAdd)
            if (project.CarCols.Lights.All(l => l.Id != light.Id))
                project.CarCols.Lights.Add(light);

        foreach (var meta in result.ExtraMetasToAdd)
        {
            var same = resource.ExtraMetas.FirstOrDefault(m => m.FileName.Equals(meta.FileName, StringComparison.OrdinalIgnoreCase));
            if (same != null && SameXml(same.Content, meta.Content)) continue;
            if (same != null)
            {
                var stem = Path.GetFileNameWithoutExtension(meta.FileName);
                var used = resource.ExtraMetas.Select(m => m.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                int n = 2;
                while (used.Contains($"{stem}_{n}.meta")) n++;
                meta.FileName = $"{stem}_{n}.meta";
            }
            resource.ExtraMetas.Add(meta);
        }

        if (result.ImportedVehicles.Count > 0 || result.ImportedSirenIds.Count > 0)
            project.IsDirty = true;
    }

    private static bool SameXml(string a, string b)
    {
        try
        {
            return System.Xml.Linq.XNode.DeepEquals(
                System.Xml.Linq.XDocument.Parse(a).Root, System.Xml.Linq.XDocument.Parse(b).Root);
        }
        catch
        {
            return a == b;
        }
    }

    private static bool SameSiren(SirenSetting a, SirenSetting b)
    {
        var svc = new MetaXmlService();
        return System.Xml.Linq.XNode.DeepEquals(svc.BuildSirenSetting(a), svc.BuildSirenSetting(b));
    }

    /// <summary>Copies (and encrypts) a source file into vehicles/&lt;vehicleName&gt;/ and returns its relative path.</summary>
    private string? StoreFile(Project project, string vehicleName, string? sourcePath, string? targetFileName = null)
    {
        if (string.IsNullOrEmpty(sourcePath)) return null;

        var vehicleDir = Path.Combine(project.FolderPath!, VehiclesFolder, vehicleName);
        Directory.CreateDirectory(vehicleDir);

        var fileName = targetFileName ?? Path.GetFileName(sourcePath);
        File.WriteAllBytes(Path.Combine(vehicleDir, fileName), EncodeStored(project, File.ReadAllBytes(sourcePath)));
        return Path.Combine(vehicleName, fileName);
    }

    public byte[] DecryptVehicleFile(Project project, Vehicle vehicle, string relativePath)
    {
        if (string.IsNullOrEmpty(project.FolderPath))
            throw new InvalidOperationException("Project has no folder path.");

        var fullPath = Path.Combine(project.FolderPath, VehiclesFolder, relativePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Vehicle file not found: {fullPath}");

        return ReadStored(fullPath);
    }

    public void ExportProjectZip(Project project, string zipPath, string password,
        IProgress<(int current, int total, string message)>? progress = null)
    {
        if (string.IsNullOrEmpty(project.FolderPath))
            throw new InvalidOperationException("Project has no folder path.");

        // Create a temporary directory with decrypted files
        var tempDir = Path.Combine(Path.GetTempPath(), $"julveh_export_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var vehiclesSrcDir = Path.Combine(project.FolderPath, VehiclesFolder);

            // Only files that are referenced by a vehicle (no leftovers of deleted vehicles)
            var allFiles = ReferencedFiles(project).Where(File.Exists).ToArray();
            var total = allFiles.Length + 1; // +1 for project file
            var current = 0;

            // Write decrypted project.julveh (re-encrypted with password)
            progress?.Report((++current, total, "Encrypting project file..."));
            var projectJson = SerializeProject(project);
            var projectBytes = Encoding.UTF8.GetBytes(projectJson);
            var encryptedProject = _encryption.EncryptWithPassword(projectBytes, password);
            File.WriteAllBytes(Path.Combine(tempDir, ProjectFileName), encryptedProject);

            // Copy and re-encrypt vehicle files with password
            if (Directory.Exists(vehiclesSrcDir))
            {
                var tempVehiclesDir = Path.Combine(tempDir, VehiclesFolder);
                foreach (var file in allFiles)
                {
                    progress?.Report((++current, total, $"Encrypting {Path.GetFileName(file)}..."));

                    var relativePath = Path.GetRelativePath(vehiclesSrcDir, file);
                    var destPath = Path.Combine(tempVehiclesDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

                    // Decrypt local storage (if encrypted), encrypt with password
                    var decrypted = ReadStored(file);
                    var reEncrypted = _encryption.EncryptWithPassword(decrypted, password);
                    File.WriteAllBytes(destPath, reEncrypted);
                }
            }

            // Create ZIP from temp directory
            if (File.Exists(zipPath))
                File.Delete(zipPath);
            ZipFile.CreateFromDirectory(tempDir, zipPath, CompressionLevel.Optimal, false);
        }
        finally
        {
            _encryption.ClearPasswordCache();
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    public Project ImportProjectZip(string zipPath, string password, string targetFolderPath,
        IProgress<(int current, int total, string message)>? progress = null)
    {
        // Extract ZIP to temp directory
        var tempDir = Path.Combine(Path.GetTempPath(), $"julveh_import_{Guid.NewGuid():N}");
        try
        {
            ZipFile.ExtractToDirectory(zipPath, tempDir);

            // Decrypt project file with password
            var projectFilePath = Path.Combine(tempDir, ProjectFileName);
            if (!File.Exists(projectFilePath))
                throw new FileNotFoundException("ZIP does not contain a valid project file.");

            progress?.Report((1, 3, "Decrypting project file..."));
            var encryptedProject = File.ReadAllBytes(projectFilePath);
            var projectJson = Encoding.UTF8.GetString(
                _encryption.DecryptWithPassword(encryptedProject, password));

            var project = JsonSerializer.Deserialize<Project>(projectJson, ProjectJsonOptions.Default)
                          ?? throw new InvalidOperationException("Failed to deserialize project.");

            // Create target directory
            Directory.CreateDirectory(targetFolderPath);
            var targetVehiclesDir = Path.Combine(targetFolderPath, VehiclesFolder);
            Directory.CreateDirectory(targetVehiclesDir);

            // Re-encrypt vehicle files with local DPAPI
            var tempVehiclesDir = Path.Combine(tempDir, VehiclesFolder);
            if (Directory.Exists(tempVehiclesDir))
            {
                var files = Directory.GetFiles(tempVehiclesDir, "*.*", SearchOption.AllDirectories);
                var total = files.Length;
                var current = 0;

                foreach (var file in files)
                {
                    progress?.Report((++current, total, $"Importing {Path.GetFileName(file)}..."));

                    var relativePath = Path.GetRelativePath(tempVehiclesDir, file);
                    var destPath = Path.Combine(targetVehiclesDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

                    // Decrypt with password, store according to the project's local encryption setting
                    var decrypted = _encryption.DecryptWithPassword(File.ReadAllBytes(file), password);
                    File.WriteAllBytes(destPath, EncodeStored(project, decrypted));
                }
            }

            // Save project with local encryption
            project.FolderPath = targetFolderPath;
            SaveProjectFile(project);
            project.IsDirty = false;

            return project;
        }
        finally
        {
            _encryption.ClearPasswordCache();
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// Renames a vehicle as one transaction: the vehicles/&lt;name&gt;/ folder, the stream files that
    /// carry the name (old.yft, old_hi.yft, old+hi.ytd, old_bumf.yft, ...) and — if
    /// <paramref name="updateMetaNames"/> — the meta names that refer to it. On any error all file
    /// operations are rolled back and an <see cref="InvalidOperationException"/> is thrown; the
    /// vehicle is left unchanged.
    /// </summary>
    public void RenameVehicle(Project project, Vehicle vehicle, string oldName, string newName, bool updateMetaNames = true)
    {
        if (oldName == newName) return;
        if (!VehicleNaming.IsValidName(newName, out var error))
            throw new InvalidOperationException(error);
        if (VehicleNaming.UsedNames(project, vehicle).Contains(newName))
            throw new InvalidOperationException($"A vehicle named '{newName}' already exists in the project.");

        if (!string.IsNullOrEmpty(project.FolderPath))
        {
            var vehiclesDir = Path.Combine(project.FolderPath, VehiclesFolder);
            var plan = PlanPaths(vehicle, oldName, newName);
            var done = new List<(string from, string to)>();

            try
            {
                var oldDir = Path.Combine(vehiclesDir, oldName);
                var newDir = Path.Combine(vehiclesDir, newName);
                if (Directory.Exists(oldDir))
                {
                    if (!oldName.Equals(newName, StringComparison.OrdinalIgnoreCase) && Directory.Exists(newDir))
                        throw new InvalidOperationException($"Folder 'vehicles/{newName}' already exists.");
                    MoveWithCase(oldDir, newDir, done, isDirectory: true);
                }

                foreach (var (oldRel, newRel) in plan.Values.Where(p => p.oldRel != p.newRel))
                {
                    // the folder was already moved, so the file is at <newFolder>/<oldFileName>
                    var from = Path.Combine(vehiclesDir, Path.GetDirectoryName(newRel) ?? "", Path.GetFileName(oldRel));
                    var to = Path.Combine(vehiclesDir, newRel);
                    if (!File.Exists(from)) continue; // reported as missing file later
                    if (File.Exists(to) && !from.Equals(to, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"File '{newRel}' already exists.");
                    MoveWithCase(from, to, done, isDirectory: false);
                }
            }
            catch (Exception ex)
            {
                Rollback(done);
                throw ex as InvalidOperationException
                      ?? new InvalidOperationException($"Rename failed: {ex.Message}", ex);
            }

            ApplyPlan(vehicle, plan);
        }

        vehicle.Name = newName;
        if (updateMetaNames)
            VehicleNaming.ApplyNameToMetas(vehicle, oldName, newName);
    }

    /// <summary>
    /// Creates an independent copy of the vehicle under <paramref name="newName"/>: stream files are
    /// copied into vehicles/&lt;newName&gt;/ with renamed file names and all meta names are updated.
    /// The clone is not added to any resource.
    /// </summary>
    public Vehicle CloneVehicle(Project project, Vehicle vehicle, string newName)
    {
        if (!VehicleNaming.IsValidName(newName, out var error))
            throw new InvalidOperationException(error);
        if (VehicleNaming.UsedNames(project).Contains(newName))
            throw new InvalidOperationException($"A vehicle named '{newName}' already exists in the project.");

        var json = JsonSerializer.Serialize(vehicle, ProjectJsonOptions.Default);
        var clone = JsonSerializer.Deserialize<Vehicle>(json, ProjectJsonOptions.Default)
                    ?? throw new InvalidOperationException("Vehicle clone failed.");

        if (!string.IsNullOrEmpty(project.FolderPath))
        {
            var vehiclesDir = Path.Combine(project.FolderPath, VehiclesFolder);
            var newDir = Path.Combine(vehiclesDir, newName);
            if (Directory.Exists(newDir) && Directory.EnumerateFileSystemEntries(newDir).Any())
                throw new InvalidOperationException($"Folder 'vehicles/{newName}' already exists.");

            var plan = PlanPaths(clone, vehicle.Name, newName, forceFolder: true);
            var copied = new List<string>();
            try
            {
                Directory.CreateDirectory(newDir);
                foreach (var (oldRel, newRel) in plan.Values)
                {
                    var from = Path.Combine(vehiclesDir, oldRel);
                    if (!File.Exists(from)) continue;
                    var to = Path.Combine(vehiclesDir, newRel);
                    File.Copy(from, to, overwrite: false);
                    copied.Add(to);
                }
            }
            catch (Exception ex)
            {
                foreach (var f in copied) TryDelete(f);
                throw new InvalidOperationException($"Clone failed: {ex.Message}", ex);
            }

            ApplyPlan(clone, plan);
        }

        var oldName = clone.Name;
        clone.Name = newName;
        VehicleNaming.ApplyNameToMetas(clone, oldName, newName);
        return clone;
    }

    private enum Slot { Yft, YftHi, Ytd, YtdHi }

    /// <summary>
    /// Computes the new relative path for every file of the vehicle. Files in vehicles/&lt;oldName&gt;/
    /// move to vehicles/&lt;newName&gt;/ (always, with <paramref name="forceFolder"/>), and file names
    /// that carry the vehicle name are renamed.
    /// </summary>
    private static Dictionary<object, (string oldRel, string newRel)> PlanPaths(
        Vehicle vehicle, string oldName, string newName, bool forceFolder = false)
    {
        var plan = new Dictionary<object, (string, string)>();

        string Map(string rel)
        {
            var folder = Path.GetDirectoryName(rel) ?? "";
            var file = Path.GetFileName(rel);
            if (forceFolder || folder.Equals(oldName, StringComparison.OrdinalIgnoreCase))
                folder = newName;
            return Path.Combine(folder, VehicleNaming.RenameFileName(file, oldName, newName));
        }

        void Add(object key, string? rel)
        {
            if (!string.IsNullOrEmpty(rel)) plan[key] = (rel, Map(rel));
        }

        Add(Slot.Yft, vehicle.YftRelativePath);
        Add(Slot.YftHi, vehicle.YftHiRelativePath);
        Add(Slot.Ytd, vehicle.YtdRelativePath);
        Add(Slot.YtdHi, vehicle.YtdHiRelativePath);
        for (int i = 0; i < vehicle.ExtraFiles.Count; i++)
            Add(i, vehicle.ExtraFiles[i]);
        return plan;
    }

    private static void ApplyPlan(Vehicle vehicle, Dictionary<object, (string oldRel, string newRel)> plan)
    {
        string? Get(Slot s, string? current) => plan.TryGetValue(s, out var p) ? p.newRel : current;
        vehicle.YftRelativePath = Get(Slot.Yft, vehicle.YftRelativePath);
        vehicle.YftHiRelativePath = Get(Slot.YftHi, vehicle.YftHiRelativePath);
        vehicle.YtdRelativePath = Get(Slot.Ytd, vehicle.YtdRelativePath);
        vehicle.YtdHiRelativePath = Get(Slot.YtdHi, vehicle.YtdHiRelativePath);
        for (int i = 0; i < vehicle.ExtraFiles.Count; i++)
            if (plan.TryGetValue(i, out var p))
                vehicle.ExtraFiles[i] = p.newRel;
    }

    /// <summary>Move that also works for case-only renames on case-insensitive file systems.</summary>
    private static void MoveWithCase(string from, string to, List<(string from, string to)> done, bool isDirectory)
    {
        if (from == to) return;

        void Move(string a, string b)
        {
            if (isDirectory) Directory.Move(a, b); else File.Move(a, b);
            done.Add((a, b));
        }

        if (from.Equals(to, StringComparison.OrdinalIgnoreCase))
        {
            var tmp = from + ".renaming_" + Guid.NewGuid().ToString("N")[..8];
            Move(from, tmp);
            Move(tmp, to);
        }
        else
        {
            Move(from, to);
        }
    }

    private static void Rollback(List<(string from, string to)> done)
    {
        for (int i = done.Count - 1; i >= 0; i--)
        {
            var (from, to) = done[i];
            try
            {
                if (Directory.Exists(to)) Directory.Move(to, from);
                else if (File.Exists(to)) File.Move(to, from);
            }
            catch { /* best effort */ }
        }
    }

    private static void TryDelete(string file)
    {
        try { File.Delete(file); } catch { /* best effort */ }
    }

    // --- Recent Projects ---

    public List<string> LoadRecentProjects()
    {
        var path = GetRecentProjectsPath();
        if (!File.Exists(path)) return [];

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void AddToRecentProjects(string folderPath)
    {
        var recent = LoadRecentProjects();
        recent.Remove(folderPath);
        recent.Insert(0, folderPath);
        if (recent.Count > MaxRecentProjects)
            recent.RemoveRange(MaxRecentProjects, recent.Count - MaxRecentProjects);

        var json = JsonSerializer.Serialize(recent, new JsonSerializerOptions { WriteIndented = true });
        var dir = Path.GetDirectoryName(GetRecentProjectsPath());
        if (dir != null) Directory.CreateDirectory(dir);
        File.WriteAllText(GetRecentProjectsPath(), json);
    }

    // --- Private Helpers ---

    private void SaveProjectFile(Project project)
    {
        if (string.IsNullOrEmpty(project.FolderPath))
            throw new InvalidOperationException("Project has no folder path.");

        var json = SerializeProject(project);
        var bytes = EncodeStored(project, Encoding.UTF8.GetBytes(json));

        // Atomic: write project.julveh.tmp, then swap it in and keep the previous file as .bak
        var projectFilePath = Path.Combine(project.FolderPath, ProjectFileName);
        WriteAtomic(projectFilePath, bytes, projectFilePath + BackupSuffix);
    }

    private static string SerializeProject(Project project)
    {
        return JsonSerializer.Serialize(project, ProjectJsonOptions.Default);
    }

    /// <summary>
    /// Assigns an imported file to a slot. Main slots are only used when the file name matches the
    /// vehicle (name.yft, name_hi.yft, name.ytd, name+hi.ytd); everything else (mod parts, extra
    /// texture dictionaries) goes to <see cref="Vehicle.ExtraFiles"/>.
    /// </summary>
    private static void AssignFileToVehicle(Vehicle vehicle, string fileName, string relativePath)
    {
        var lower = fileName.ToLowerInvariant();
        var name = vehicle.Name.ToLowerInvariant();

        if (lower == name + "_hi.yft")
            vehicle.YftHiRelativePath = relativePath;
        else if (lower == name + ".yft")
            vehicle.YftRelativePath = relativePath;
        else if (lower == name + "+hi.ytd")
            vehicle.YtdHiRelativePath = relativePath;
        else if (lower == name + ".ytd")
            vehicle.YtdRelativePath = relativePath;
        else if (!vehicle.ExtraFiles.Contains(relativePath, StringComparer.OrdinalIgnoreCase))
            vehicle.ExtraFiles.Add(relativePath);
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destDir = Path.Combine(destinationDir, Path.GetFileName(dir));
            CopyDirectory(dir, destDir);
        }
    }

    private static string GetRecentProjectsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "JulschaVehicleTool", RecentProjectsFileName);
    }
}
