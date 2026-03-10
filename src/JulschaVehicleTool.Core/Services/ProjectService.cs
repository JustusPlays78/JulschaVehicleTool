using System.IO.Compression;
using System.Text;
using System.Text.Json;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Serialization;

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

    byte[] DecryptVehicleFile(Project project, Vehicle vehicle, string relativePath);

    void ExportProjectZip(Project project, string zipPath, string password,
        IProgress<(int current, int total, string message)>? progress = null);
    Project ImportProjectZip(string zipPath, string password, string targetFolderPath,
        IProgress<(int current, int total, string message)>? progress = null);

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

    private static readonly string[] KnownYftExtensions = [".yft"];
    private static readonly string[] KnownYtdExtensions = [".ytd"];
    private static readonly string[] KnownMetaFiles =
        ["handling.meta", "vehicles.meta", "carvariations.meta", "carcols.meta", "vehiclelayouts.meta"];

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
        return project;
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
        if (string.IsNullOrEmpty(project.FolderPath))
            throw new InvalidOperationException("Project must be saved before importing files.");

        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
        var yftFiles = files.Where(f => f.EndsWith(".yft", StringComparison.OrdinalIgnoreCase)).ToArray();

        // Determine vehicle names from YFT files (without _hi suffix)
        var vehicleNames = yftFiles
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Where(n => !n.EndsWith("_hi", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();

        var total = vehicleNames.Count;
        var current = 0;

        foreach (var name in vehicleNames)
        {
            current++;
            progress?.Report((current, total, $"Importing {name}..."));

            // Create vehicle with defaults
            var vehicle = VehicleDefaults.CreateDefault(name);

            // Find matching files for this vehicle
            var matchingFiles = files.Where(f =>
            {
                var fn = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                var baseName = name.ToLowerInvariant();
                return fn == baseName
                    || fn == baseName + "_hi"
                    || fn == baseName + "+hi"
                    || fn.StartsWith(baseName + ".", StringComparison.OrdinalIgnoreCase);
            }).ToArray();

            // Import the binary files
            var binaryFiles = matchingFiles
                .Where(f => KnownYftExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                         || KnownYtdExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            if (binaryFiles.Length > 0)
                ImportVehicleFiles(project, vehicle, binaryFiles);

            resource.Vehicles.Add(vehicle);
        }

        // Handle meta files separately — they may contain multiple vehicle entries
        // These are returned for the caller to process via MetaImportMatchService
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

    private static void AssignFileToVehicle(Vehicle vehicle, string fileName, string relativePath)
    {
        var lower = fileName.ToLowerInvariant();

        if (lower.EndsWith("_hi.yft"))
            vehicle.YftHiRelativePath = relativePath;
        else if (lower.EndsWith(".yft"))
            vehicle.YftRelativePath = relativePath;
        else if (lower.Contains("+hi") && lower.EndsWith(".ytd"))
            vehicle.YtdHiRelativePath = relativePath;
        else if (lower.EndsWith(".ytd"))
            vehicle.YtdRelativePath = relativePath;
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
