using System.Text;
using System.Text.RegularExpressions;
using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.Core.Services;

public class FiveMExportService
{
    private const long StreamFileSizeLimit = 16 * 1024 * 1024; // 16 MB

    /// <summary>
    /// Exports a FiveM resource from a Resource with inline meta data.
    /// Generates XML meta files from inline data and decrypts binary files.
    /// </summary>
    public ExportResult ExportResource(FiveMExportOptions options,
        IProgress<(int current, int total, string message)>? progress = null)
    {
        var result = new ExportResult();
        var resource = options.Resource;
        var project = options.Project;

        if (resource.Vehicles.Count == 0)
        {
            result.Error = "No vehicles to export.";
            return result;
        }

        var outputDir = options.OutputPath;
        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(Path.Combine(outputDir, "stream"));
        Directory.CreateDirectory(Path.Combine(outputDir, "data"));

        var usedStreamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var total = resource.Vehicles.Count * 2 + 2; // stream + meta per vehicle + manifest + names
        var current = 0;

        var metaXmlService = options.MetaXmlService;
        var projectService = options.ProjectService;

        foreach (var vehicle in resource.Vehicles)
        {
            progress?.Report((++current, total, $"Copying stream files for {vehicle.Name}..."));
            CopyStreamFilesFromProject(project, vehicle, outputDir, result, usedStreamNames, projectService);

            progress?.Report((++current, total, $"Generating meta files for {vehicle.Name}..."));
            GenerateMetaFilesFromInline(vehicle, outputDir, result, usedFolderNames, metaXmlService);
        }

        // Generate a single project-level carcols.meta filtered to IDs referenced by this resource
        bool hasCarCols = GenerateProjectCarColsFile(project, resource, outputDir, metaXmlService);

        progress?.Report((++current, total, "Generating fxmanifest.lua..."));
        GenerateResourceManifest(resource, outputDir, hasCarCols);

        if (resource.IncludeVehicleNames)
        {
            progress?.Report((++current, total, "Generating vehicle_names.lua..."));
            GenerateResourceVehicleNames(resource, outputDir);
        }

        result.Success = true;
        result.OutputPath = outputDir;
        return result;
    }

    private void CopyStreamFilesFromProject(Project project, Vehicle vehicle, string outputDir,
        ExportResult result, HashSet<string> usedNames, IProjectService projectService)
    {
        var streamDir = Path.Combine(outputDir, "stream");
        var relativePaths = new[]
        {
            vehicle.YftRelativePath,
            vehicle.YftHiRelativePath,
            vehicle.YtdRelativePath,
            vehicle.YtdHiRelativePath
        };

        foreach (var relPath in relativePaths)
        {
            if (string.IsNullOrEmpty(relPath)) continue;

            try
            {
                var fileName = Path.GetFileName(relPath);

                if (!usedNames.Add(fileName))
                {
                    result.Warnings.Add(
                        $"COLLISION: {fileName} ({vehicle.Name}) overwrites a file from another vehicle in stream/.");
                }

                var decryptedBytes = projectService.DecryptVehicleFile(project, vehicle, relPath);

                if (decryptedBytes.Length > StreamFileSizeLimit)
                {
                    result.Warnings.Add(
                        $"WARNING: {fileName} ({vehicle.Name}) is {decryptedBytes.Length / (1024.0 * 1024.0):F1} MB - exceeds FiveM 16 MB stream limit.");
                }

                File.WriteAllBytes(Path.Combine(streamDir, fileName), decryptedBytes);
            }
            catch (FileNotFoundException)
            {
                result.Warnings.Add($"Missing file: {relPath} for vehicle {vehicle.Name}");
            }
        }
    }

    private void GenerateMetaFilesFromInline(Vehicle vehicle, string outputDir,
        ExportResult result, HashSet<string> usedFolders, MetaXmlService metaXmlService)
    {
        var folderName = SanitizeFolderName(vehicle.Name);
        if (!usedFolders.Add(folderName))
        {
            result.Warnings.Add(
                $"COLLISION: Vehicle '{vehicle.Name}' maps to data folder '{folderName}/' which is already used.");
        }

        var vehicleDataDir = Path.Combine(outputDir, "data", folderName);
        Directory.CreateDirectory(vehicleDataDir);

        if (vehicle.Handling != null)
            metaXmlService.SaveHandling(vehicle.Handling, Path.Combine(vehicleDataDir, "handling.meta"));

        if (vehicle.VehicleMeta != null)
            metaXmlService.SaveVehicleMeta(vehicle.VehicleMeta, Path.Combine(vehicleDataDir, "vehicles.meta"));

        if (vehicle.CarVariation != null)
            metaXmlService.SaveCarVariations(vehicle.CarVariation, Path.Combine(vehicleDataDir, "carvariations.meta"));
        // Note: carcols is exported at resource level (data/carcols.meta), not per-vehicle
    }

    /// <summary>
    /// Generates a single data/carcols.meta for the resource, containing only the siren groups
    /// referenced by vehicles in this resource.  Returns true if the file was written.
    /// </summary>
    private static bool GenerateProjectCarColsFile(Project project, Resource resource,
        string outputDir, MetaXmlService metaXmlService)
    {
        if (project.CarCols.SirenSettings.Count == 0) return false;

        // Collect all siren IDs referenced by vehicles in this resource
        var referencedIds = resource.Vehicles
            .Select(v => v.CarVariation?.SirenSettings ?? 0)
            .Where(id => id != 0)
            .ToHashSet();

        if (referencedIds.Count == 0) return false;

        var filtered = new CarColsData();
        foreach (var s in project.CarCols.SirenSettings.Where(s => referencedIds.Contains(s.Id)))
            filtered.SirenSettings.Add(s);

        if (filtered.SirenSettings.Count == 0) return false;

        metaXmlService.SaveCarCols(filtered, Path.Combine(outputDir, "data", "carcols.meta"));
        return true;
    }

    private void GenerateResourceManifest(Resource resource, string outputDir, bool hasProjectCarCols = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine("fx_version 'cerulean'");
        sb.AppendLine("game { 'gta5' }");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(resource.Author))
            sb.AppendLine($"author '{EscapeLua(resource.Author)}'");
        if (!string.IsNullOrWhiteSpace(resource.Name))
            sb.AppendLine($"description '{EscapeLua(resource.Name)}'");
        if (!string.IsNullOrWhiteSpace(resource.Version))
            sb.AppendLine($"version '{EscapeLua(resource.Version)}'");

        sb.AppendLine();
        sb.AppendLine("files {");
        sb.AppendLine("    'data/**/*.meta'");
        sb.AppendLine("}");
        sb.AppendLine();

        bool hasHandling = false, hasCarVariations = false, hasVehicles = false;

        foreach (var v in resource.Vehicles)
        {
            if (v.Handling != null) hasHandling = true;
            if (v.CarVariation != null) hasCarVariations = true;
            if (v.VehicleMeta != null) hasVehicles = true;
        }

        if (hasHandling)
            sb.AppendLine("data_file 'HANDLING_FILE'           'data/**/handling.meta'");
        if (hasProjectCarCols)
            sb.AppendLine("data_file 'CARCOLS_FILE'            'data/carcols.meta'");
        if (hasCarVariations)
            sb.AppendLine("data_file 'VEHICLE_VARIATION_FILE'  'data/**/carvariations.meta'");
        if (hasVehicles)
            sb.AppendLine("data_file 'VEHICLE_METADATA_FILE'   'data/**/vehicles.meta'");

        if (resource.IncludeVehicleNames)
        {
            sb.AppendLine();
            sb.AppendLine("client_script 'vehicle_names.lua'");
        }

        File.WriteAllText(Path.Combine(outputDir, "fxmanifest.lua"), sb.ToString());
    }

    private void GenerateResourceVehicleNames(Resource resource, string outputDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- Vehicle display names");

        foreach (var v in resource.Vehicles)
        {
            var modelName = v.VehicleMeta?.ModelName ?? v.Name;
            var displayName = v.VehicleMeta?.GameName ?? v.Name;
            sb.AppendLine($"AddTextEntry('{EscapeLua(modelName)}', '{EscapeLua(displayName)}')");
        }

        File.WriteAllText(Path.Combine(outputDir, "vehicle_names.lua"), sb.ToString());
    }

    /// <summary>
    /// Exports a single vehicle's meta data to an individual XML file.
    /// </summary>
    public void ExportSingleMeta(Vehicle vehicle, string metaType, string filePath, MetaXmlService metaXmlService)
    {
        switch (metaType.ToLowerInvariant())
        {
            case "handling" when vehicle.Handling != null:
                metaXmlService.SaveHandling(vehicle.Handling, filePath);
                break;
            case "vehicles" when vehicle.VehicleMeta != null:
                metaXmlService.SaveVehicleMeta(vehicle.VehicleMeta, filePath);
                break;
            case "carvariations" when vehicle.CarVariation != null:
                metaXmlService.SaveCarVariations(vehicle.CarVariation, filePath);
                break;
            case "carcols":
                // carcols is project-level — caller should export via GenerateProjectCarColsFile
                // kept for direct single-meta export fallback
                if (vehicle.CarCols != null)
                    metaXmlService.SaveCarCols(vehicle.CarCols, filePath);
                break;
        }
    }

    private static string SanitizeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c).ToArray());
        sanitized = Regex.Replace(sanitized.ToLowerInvariant(), "_+", "_").Trim('_');
        return string.IsNullOrWhiteSpace(sanitized) ? "vehicle" : sanitized;
    }

    private static string EscapeLua(string value)
    {
        return value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "").Replace("\r", "");
    }
}

/// <summary>Export options for the Resource-based export.</summary>
public class FiveMExportOptions
{
    public required Resource Resource { get; set; }
    public required Project Project { get; set; }
    public required string OutputPath { get; set; }
    public required MetaXmlService MetaXmlService { get; set; }
    public required IProjectService ProjectService { get; set; }
}

public class ExportResult
{
    public bool Success { get; set; }
    public string OutputPath { get; set; } = "";
    public List<string> Warnings { get; } = new();
    public string? Error { get; set; }
}
