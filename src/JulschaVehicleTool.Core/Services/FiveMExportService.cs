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
        CleanGeneratedOutput(outputDir, result);
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

        // Generate a single project-level carcols.meta filtered to what this resource references
        bool hasCarCols = GenerateProjectCarColsFile(project, resource, outputDir, metaXmlService);

        // Pass-through metas (vehiclelayouts.meta, ...)
        foreach (var meta in resource.ExtraMetas)
        {
            var dir = Path.Combine(outputDir, "data", "extra");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, SanitizeFileName(meta.FileName)), meta.Content);
        }

        progress?.Report((++current, total, "Generating fxmanifest.lua..."));
        GenerateResourceManifest(resource, outputDir, hasCarCols);

        if (resource.IncludeVehicleNames)
        {
            progress?.Report((++current, total, "Generating vehicle_names.lua..."));
            GenerateResourceVehicleNames(resource, outputDir);
        }

        File.WriteAllText(Path.Combine(outputDir, ExportMarkerFile),
            "Exported by Julscha Vehicle Tool. stream/, data/, fxmanifest.lua and vehicle_names.lua are replaced on every export.\n");

        result.Success = true;
        result.OutputPath = outputDir;
        return result;
    }

    private void CopyStreamFilesFromProject(Project project, Vehicle vehicle, string outputDir,
        ExportResult result, HashSet<string> usedNames, IProjectService projectService)
    {
        var streamDir = Path.Combine(outputDir, "stream");
        foreach (var relPath in vehicle.AllStreamFiles)
        {
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
    /// <summary>
    /// Removes what a previous export generated (stream/, data/, fxmanifest.lua, vehicle_names.lua)
    /// so files of removed vehicles don't linger. Anything else in the folder (own scripts, ...) is kept.
    /// </summary>
    private const string ExportMarkerFile = ".julschavehicletool";

    private static void CleanGeneratedOutput(string outputDir, ExportResult result)
    {
        if (!Directory.Exists(outputDir)) return;

        // Only clean folders this tool exported before — never delete someone else's stream/ or data/
        if (!File.Exists(Path.Combine(outputDir, ExportMarkerFile)))
        {
            if (Directory.Exists(Path.Combine(outputDir, "stream")) || Directory.Exists(Path.Combine(outputDir, "data")))
                result.Warnings.Add("The output folder already contains stream/ or data/ that were not created by this tool — " +
                                    "old files were not removed. Export into an empty folder to be safe.");
            return;
        }

        foreach (var dir in new[] { "stream", "data" })
        {
            var path = Path.Combine(outputDir, dir);
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
        foreach (var file in new[] { "fxmanifest.lua", "vehicle_names.lua" })
        {
            var path = Path.Combine(outputDir, file);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>
    /// The part of the project's carcols pool that the given vehicles use:
    /// siren groups (sirenSettings), mod kits (kits by kitName) and light settings (lightSettings).
    /// </summary>
    public static CarColsData FilterCarCols(Project project, IEnumerable<Vehicle> vehicles)
    {
        var list = vehicles.ToList();
        var sirenIds = list.Select(v => v.CarVariation?.SirenSettings ?? 0).Where(id => id != 0).ToHashSet();
        var kitNames = list.SelectMany(v => v.CarVariation?.Kits ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var lightIds = list.Where(v => v.CarVariation != null).Select(v => v.CarVariation!.LightSettings).ToHashSet();

        var filtered = new CarColsData();
        foreach (var s in project.CarCols.SirenSettings.Where(s => sirenIds.Contains(s.Id)))
            filtered.SirenSettings.Add(s);
        foreach (var k in project.CarCols.Kits.Where(k => kitNames.Contains(k.Name)))
            filtered.Kits.Add(k);
        foreach (var l in project.CarCols.Lights.Where(l => lightIds.Contains(l.Id)))
            filtered.Lights.Add(l);
        return filtered;
    }

    private static bool GenerateProjectCarColsFile(Project project, Resource resource,
        string outputDir, MetaXmlService metaXmlService)
    {
        var filtered = FilterCarCols(project, resource.Vehicles);
        if (filtered.SirenSettings.Count == 0 && filtered.Kits.Count == 0 && filtered.Lights.Count == 0)
            return false;

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
        var description = string.IsNullOrWhiteSpace(resource.Description) ? resource.Name : resource.Description;
        if (!string.IsNullOrWhiteSpace(description))
            sb.AppendLine($"description '{EscapeLua(description)}'");
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

        // Order matters: layouts before vehicles, carcols before carvariations
        foreach (var meta in resource.ExtraMetas)
            sb.AppendLine($"data_file '{meta.DataFileType}' 'data/extra/{SanitizeFileName(meta.FileName)}'");
        if (hasHandling)
            sb.AppendLine("data_file 'HANDLING_FILE'           'data/**/handling.meta'");
        if (hasVehicles)
            sb.AppendLine("data_file 'VEHICLE_METADATA_FILE'   'data/**/vehicles.meta'");
        if (hasProjectCarCols)
            sb.AppendLine("data_file 'CARCOLS_FILE'            'data/carcols.meta'");
        if (hasCarVariations)
            sb.AppendLine("data_file 'VEHICLE_VARIATION_FILE'  'data/**/carvariations.meta'");

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

        sb.AppendLine("-- gameName (vehicles.meta) is the text label key; the value is the name shown in game.");
        sb.AppendLine("Citizen.CreateThread(function()");

        foreach (var v in resource.Vehicles)
        {
            var label = v.VehicleMeta?.GameName;
            if (string.IsNullOrWhiteSpace(label)) label = v.VehicleMeta?.ModelName;
            if (string.IsNullOrWhiteSpace(label)) label = v.Name;

            var displayName = v.VehicleMeta?.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName)) displayName = label;

            sb.AppendLine($"    AddTextEntry('{EscapeLua(label)}', '{EscapeLua(displayName)}')");
        }

        sb.AppendLine("end)");

        File.WriteAllText(Path.Combine(outputDir, "vehicle_names.lua"), sb.ToString());
    }

    /// <summary>
    /// Exports a single vehicle's meta data to an individual XML file.
    /// </summary>
    public void ExportSingleMeta(Project project, Vehicle vehicle, string metaType, string filePath, MetaXmlService metaXmlService)
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
                // carcols is project-level: export the sirens/kits/lights this vehicle uses
                metaXmlService.SaveCarCols(FilterCarCols(project, [vehicle]), filePath);
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

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) || c is '/' or '\\' ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "extra.meta" : cleaned;
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
