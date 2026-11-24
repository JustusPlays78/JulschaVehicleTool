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

    void RenameVehicle(Project project, Vehicle vehicle, string oldName, string newName);
    Vehicle CloneVehicle(Vehicle vehicle);

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

        var encryptedBytes = File.ReadAllBytes(projectFilePath);
        var jsonBytes = _encryption.DecryptLocal(encryptedBytes);
        var json = Encoding.UTF8.GetString(jsonBytes);

        var project = JsonSerializer.Deserialize<Project>(json, ProjectJsonOptions.Default)
                      ?? throw new InvalidOperationException("Failed to deserialize project file.");

        project.FolderPath = folderPath;
        project.IsDirty = false;

        // Migrate old per-vehicle CarCols to the project-level pool (one-time, backward compat)
        MigrateVehicleCarColsToProject(project);

        return project;
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

            // Read, encrypt, and write
            var plainBytes = File.ReadAllBytes(sourcePath);
            var encryptedBytes = _encryption.EncryptLocal(plainBytes);
            File.WriteAllBytes(destPath, encryptedBytes);

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

        foreach (var candidate in selected)
        {
            progress?.Report((++current, selected.Count, $"Importing {candidate.Name}..."));

            if (!existingNames.Add(candidate.Name))
            {
                result.Warnings.Add($"Skipped '{candidate.Name}': a vehicle with this name already exists in the project.");
                continue;
            }

            var vehicle = VehicleDefaults.CreateDefault(candidate.Name);
            vehicle.CarCols = null; // sirens live in the project pool
            if (candidate.VehicleMeta != null) vehicle.VehicleMeta = candidate.VehicleMeta;
            if (candidate.Handling != null) vehicle.Handling = candidate.Handling;
            if (candidate.CarVariation != null) vehicle.CarVariation = candidate.CarVariation;
            if (vehicle.VehicleMeta != null && string.IsNullOrEmpty(vehicle.VehicleMeta.DisplayName))
                vehicle.VehicleMeta.DisplayName = vehicle.VehicleMeta.GameName;

            vehicle.YftRelativePath = StoreFile(project, vehicle.Name, candidate.Model);
            vehicle.YftHiRelativePath = StoreFile(project, vehicle.Name, candidate.ModelHi);
            vehicle.YtdRelativePath = StoreFile(project, vehicle.Name, candidate.Texture);
            vehicle.YtdHiRelativePath = StoreFile(project, vehicle.Name, candidate.TextureHi);
            foreach (var extra in candidate.ModParts.Concat(candidate.ExtraTextures))
                vehicle.ExtraFiles.Add(StoreFile(project, vehicle.Name, extra)!);

            result.ImportedVehicles.Add(vehicle);
        }

        // Siren groups: only those referenced by imported vehicles
        var referenced = result.ImportedVehicles
            .Select(v => v.CarVariation?.SirenSettings ?? 0).Where(id => id != 0).ToHashSet();
        foreach (var siren in scan.CarCols.SirenSettings.Where(s => referenced.Contains(s.Id)))
        {
            var existing = project.CarCols.SirenSettings.FirstOrDefault(s => s.Id == siren.Id);
            if (existing == null)
                result.SirensToAdd.Add(siren);
            else if (!SameSiren(existing, siren))
                result.Warnings.Add(
                    $"Siren setting {siren.Id} ('{siren.Name}') already exists in the project with different content — kept the existing one.");
        }

        return result;
    }

    public void ApplyImport(Project project, Resource resource, ImportResult result)
    {
        foreach (var vehicle in result.ImportedVehicles)
            resource.Vehicles.Add(vehicle);

        foreach (var siren in result.SirensToAdd)
        {
            if (project.CarCols.SirenSettings.Any(s => s.Id == siren.Id)) continue;
            project.CarCols.SirenSettings.Add(siren);
            result.ImportedSirenIds.Add(siren.Id);
        }

        if (result.ImportedVehicles.Count > 0 || result.ImportedSirenIds.Count > 0)
            project.IsDirty = true;
    }

    private static bool SameSiren(SirenSetting a, SirenSetting b)
    {
        var svc = new MetaXmlService();
        return System.Xml.Linq.XNode.DeepEquals(svc.BuildSirenSetting(a), svc.BuildSirenSetting(b));
    }

    /// <summary>Copies (and encrypts) a source file into vehicles/&lt;vehicleName&gt;/ and returns its relative path.</summary>
    private string? StoreFile(Project project, string vehicleName, string? sourcePath)
    {
        if (string.IsNullOrEmpty(sourcePath)) return null;

        var vehicleDir = Path.Combine(project.FolderPath!, VehiclesFolder, vehicleName);
        Directory.CreateDirectory(vehicleDir);

        var fileName = Path.GetFileName(sourcePath);
        File.WriteAllBytes(Path.Combine(vehicleDir, fileName), _encryption.EncryptLocal(File.ReadAllBytes(sourcePath)));
        return Path.Combine(vehicleName, fileName);
    }

    public byte[] DecryptVehicleFile(Project project, Vehicle vehicle, string relativePath)
    {
        if (string.IsNullOrEmpty(project.FolderPath))
            throw new InvalidOperationException("Project has no folder path.");

        var fullPath = Path.Combine(project.FolderPath, VehiclesFolder, relativePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Vehicle file not found: {fullPath}");

        var encryptedBytes = File.ReadAllBytes(fullPath);
        return _encryption.DecryptLocal(encryptedBytes);
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

            // Count total files for progress
            var allFiles = Directory.Exists(vehiclesSrcDir)
                ? Directory.GetFiles(vehiclesSrcDir, "*.*", SearchOption.AllDirectories)
                : Array.Empty<string>();
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

                    // Decrypt from DPAPI, re-encrypt with password
                    var decrypted = _encryption.DecryptLocal(File.ReadAllBytes(file));
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

                    // Decrypt with password, re-encrypt with DPAPI
                    var decrypted = _encryption.DecryptWithPassword(File.ReadAllBytes(file), password);
                    var localEncrypted = _encryption.EncryptLocal(decrypted);
                    File.WriteAllBytes(destPath, localEncrypted);
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
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    public void RenameVehicle(Project project, Vehicle vehicle, string oldName, string newName)
    {
        if (string.IsNullOrEmpty(project.FolderPath) || oldName == newName) return;

        var vehiclesDir = Path.Combine(project.FolderPath, VehiclesFolder);
        var oldDir = Path.Combine(vehiclesDir, oldName);
        var newDir = Path.Combine(vehiclesDir, newName);

        if (Directory.Exists(oldDir) && !Directory.Exists(newDir))
            Directory.Move(oldDir, newDir);

        static string? UpdatePath(string? path, string oldN, string newN)
        {
            if (path == null) return null;
            var sep = path.IndexOfAny(['/', '\\']);
            if (sep < 0) return path;
            var first = path[..sep];
            return string.Equals(first, oldN, StringComparison.OrdinalIgnoreCase)
                ? newN + path[sep..] : path;
        }

        vehicle.YftRelativePath   = UpdatePath(vehicle.YftRelativePath,   oldName, newName);
        vehicle.YftHiRelativePath = UpdatePath(vehicle.YftHiRelativePath, oldName, newName);
        vehicle.YtdRelativePath   = UpdatePath(vehicle.YtdRelativePath,   oldName, newName);
        vehicle.YtdHiRelativePath = UpdatePath(vehicle.YtdHiRelativePath, oldName, newName);
        for (int i = 0; i < vehicle.ExtraFiles.Count; i++)
            vehicle.ExtraFiles[i] = UpdatePath(vehicle.ExtraFiles[i], oldName, newName)!;
    }

    public Vehicle CloneVehicle(Vehicle vehicle)
    {
        var json = JsonSerializer.Serialize(vehicle, ProjectJsonOptions.Default);
        return JsonSerializer.Deserialize<Vehicle>(json, ProjectJsonOptions.Default)
            ?? throw new InvalidOperationException("Vehicle clone failed.");
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
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var encrypted = _encryption.EncryptLocal(jsonBytes);

        var projectFilePath = Path.Combine(project.FolderPath, ProjectFileName);
        File.WriteAllBytes(projectFilePath, encrypted);
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
