using System.Text;
using System.Text.RegularExpressions;
using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.Core.Services;

public class FiveMExportService
{
    private const long StreamFileSizeLimit = 16 * 1024 * 1024; // 16 MB

    public ExportResult Export(ExportOptions options)
    {
        var result = new ExportResult();
        var outputDir = options.OutputPath;

        if (options.Vehicles.Count == 0)
        {
            result.Error = "No vehicles to export.";
            return result;
        }

        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(Path.Combine(outputDir, "stream"));
        Directory.CreateDirectory(Path.Combine(outputDir, "data"));

        var usedStreamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Process each vehicle
        foreach (var vehicle in options.Vehicles)
        {
            CopyStreamFiles(vehicle, outputDir, result, usedStreamNames);
            CopyMetaFiles(vehicle, outputDir, result, usedFolderNames);
        }

        // Generate fxmanifest.lua
        GenerateManifest(options, outputDir);

        // Generate vehicle_names.lua (optional)
        if (options.IncludeVehicleNames)
            GenerateVehicleNames(options, outputDir);

        result.Success = true;
        result.OutputPath = outputDir;
        return result;
    }

    private void CopyStreamFiles(VehicleEntry vehicle, string outputDir, ExportResult result, HashSet<string> usedNames)
    {
        var streamDir = Path.Combine(outputDir, "stream");

        var streamFiles = new[]
        {
            vehicle.YftFilePath,
            vehicle.YftHiFilePath,
            vehicle.YtdFilePath,
            vehicle.YtdHiFilePath
        };

        foreach (var filePath in streamFiles)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                continue;

            var fi = new FileInfo(filePath);

            if (!usedNames.Add(fi.Name))
            {
                result.Warnings.Add(
                    $"COLLISION: {fi.Name} ({vehicle.Name}) overwrites a file from another vehicle in stream/. " +
                    "Rename one of the YFT/YTD files to avoid this.");
            }

            if (fi.Length > StreamFileSizeLimit)
            {
                result.Warnings.Add(
                    $"WARNING: {fi.Name} ({vehicle.Name}) is {fi.Length / (1024.0 * 1024.0):F1} MB - exceeds FiveM 16 MB stream limit. " +
                    "This may cause crashes. Consider compressing textures or splitting the YTD.");
            }

            File.Copy(filePath, Path.Combine(streamDir, fi.Name), overwrite: true);
        }
    }

    private void CopyMetaFiles(VehicleEntry vehicle, string outputDir, ExportResult result, HashSet<string> usedFolders)
    {
        // Each vehicle gets its own subfolder under data/
        var folderName = SanitizeFolderName(vehicle.Name);
        if (!usedFolders.Add(folderName))
        {
            result.Warnings.Add(
                $"COLLISION: Vehicle '{vehicle.Name}' maps to data folder '{folderName}/' which is already used by another vehicle. " +
                "Meta files will be overwritten. Rename one of the vehicles.");
        }
        var vehicleDataDir = Path.Combine(outputDir, "data", folderName);
        Directory.CreateDirectory(vehicleDataDir);

        var metaFiles = new[]
        {
            vehicle.HandlingMetaPath,
            vehicle.VehiclesMetaPath,
            vehicle.CarVariationsMetaPath,
            vehicle.CarColsMetaPath,
            vehicle.VehicleLayoutsMetaPath
        };

        foreach (var filePath in metaFiles)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                continue;

            File.Copy(filePath, Path.Combine(vehicleDataDir, Path.GetFileName(filePath)), overwrite: true);
        }
    }

    private void GenerateManifest(ExportOptions options, string outputDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("fx_version 'cerulean'");
        sb.AppendLine("game { 'gta5' }");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(options.Author))
            sb.AppendLine($"author '{EscapeLua(options.Author)}'");
        if (!string.IsNullOrWhiteSpace(options.ResourceName))
            sb.AppendLine($"description '{EscapeLua(options.ResourceName)}'");
        if (!string.IsNullOrWhiteSpace(options.Version))
            sb.AppendLine($"version '{EscapeLua(options.Version)}'");

        sb.AppendLine();
        sb.AppendLine("files {");
        sb.AppendLine("    'data/**/*.meta'");
        sb.AppendLine("}");
        sb.AppendLine();

        // Determine which meta types exist across all vehicles
        bool hasHandling = false, hasCarCols = false, hasCarVariations = false;
        bool hasVehicleLayouts = false, hasVehicles = false;

        foreach (var vehicle in options.Vehicles)
        {
            if (!string.IsNullOrEmpty(vehicle.HandlingMetaPath) && File.Exists(vehicle.HandlingMetaPath))
                hasHandling = true;
            if (!string.IsNullOrEmpty(vehicle.CarColsMetaPath) && File.Exists(vehicle.CarColsMetaPath))
                hasCarCols = true;
            if (!string.IsNullOrEmpty(vehicle.CarVariationsMetaPath) && File.Exists(vehicle.CarVariationsMetaPath))
                hasCarVariations = true;
            if (!string.IsNullOrEmpty(vehicle.VehicleLayoutsMetaPath) && File.Exists(vehicle.VehicleLayoutsMetaPath))
                hasVehicleLayouts = true;
            if (!string.IsNullOrEmpty(vehicle.VehiclesMetaPath) && File.Exists(vehicle.VehiclesMetaPath))
                hasVehicles = true;
        }

        // CRITICAL: data_file order matters!
        // vehiclelayouts MUST be before vehicles.meta
        // vehicles.meta MUST be LAST

        if (hasHandling)
            sb.AppendLine("data_file 'HANDLING_FILE'           'data/**/handling.meta'");

        if (hasCarCols)
            sb.AppendLine("data_file 'CARCOLS_FILE'            'data/**/carcols.meta'");

        if (hasCarVariations)
            sb.AppendLine("data_file 'VEHICLE_VARIATION_FILE'  'data/**/carvariations.meta'");

        // vehiclelayouts BEFORE vehicles.meta!
        if (hasVehicleLayouts)
            sb.AppendLine("data_file 'VEHICLE_LAYOUTS_FILE'    'data/**/vehiclelayouts.meta'");

        // vehicles.meta ALWAYS last!
        if (hasVehicles)
            sb.AppendLine("data_file 'VEHICLE_METADATA_FILE'   'data/**/vehicles.meta'");

        // client_script for vehicle_names.lua
        if (options.IncludeVehicleNames)
        {
            sb.AppendLine();
            sb.AppendLine("client_script 'vehicle_names.lua'");
        }

        File.WriteAllText(Path.Combine(outputDir, "fxmanifest.lua"), sb.ToString());
    }

    private void GenerateVehicleNames(ExportOptions options, string outputDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- Vehicle display names");

        foreach (var vehicle in options.Vehicles)
        {
            // Use YFT filename as model name, VehicleEntry.Name as display name
            var modelName = !string.IsNullOrEmpty(vehicle.YftFilePath)
                ? Path.GetFileNameWithoutExtension(vehicle.YftFilePath)
                : SanitizeFolderName(vehicle.Name);

            sb.AppendLine($"AddTextEntry('{EscapeLua(modelName)}', '{EscapeLua(vehicle.Name)}')");
        }

        File.WriteAllText(Path.Combine(outputDir, "vehicle_names.lua"), sb.ToString());
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

public class ExportOptions
{
    public string ResourceName { get; set; } = "";
    public string Author { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string OutputPath { get; set; } = "";
    public bool IncludeVehicleNames { get; set; } = true;
    public List<VehicleEntry> Vehicles { get; set; } = new();
}

public class ExportResult
{
    public bool Success { get; set; }
    public string OutputPath { get; set; } = "";
    public List<string> Warnings { get; } = new();
    public string? Error { get; set; }
}
