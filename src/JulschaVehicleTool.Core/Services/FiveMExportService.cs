using System.Text;
using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.Core.Services;

public class FiveMExportService
{
    private const long StreamFileSizeLimit = 16 * 1024 * 1024; // 16 MB

    public ExportResult Export(ExportOptions options)
    {
        var result = new ExportResult();
        var outputDir = options.OutputPath;

        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(Path.Combine(outputDir, "stream"));
        Directory.CreateDirectory(Path.Combine(outputDir, "data"));

        // Copy stream files (YFT, YTD) and check 16 MB limit
        CopyStreamFiles(options, result);

        // Copy meta files to data/
        CopyMetaFiles(options, result);

        // Generate fxmanifest.lua
        GenerateManifest(options, outputDir);

        // Generate vehicle_names.lua (optional)
        if (options.IncludeVehicleNames && !string.IsNullOrWhiteSpace(options.VehicleDisplayName))
            GenerateVehicleNames(options, outputDir);

        result.Success = true;
        result.OutputPath = outputDir;
        return result;
    }

    private void CopyStreamFiles(ExportOptions options, ExportResult result)
    {
        var streamDir = Path.Combine(options.OutputPath, "stream");

        var streamFiles = new[]
        {
            options.YftPath,
            options.YftHiPath,
            options.YtdPath,
            options.YtdHiPath
        };

        foreach (var filePath in streamFiles)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                continue;

            var fi = new FileInfo(filePath);

            if (fi.Length > StreamFileSizeLimit)
            {
                result.Warnings.Add(
                    $"WARNING: {fi.Name} is {fi.Length / (1024.0 * 1024.0):F1} MB - exceeds FiveM 16 MB stream limit. " +
                    "This may cause crashes. Consider compressing textures or splitting the YTD.");
            }

            File.Copy(filePath, Path.Combine(streamDir, fi.Name), overwrite: true);
        }
    }

    private void CopyMetaFiles(ExportOptions options, ExportResult result)
    {
        var dataDir = Path.Combine(options.OutputPath, "data");

        var metaFiles = new[]
        {
            options.HandlingMetaPath,
            options.VehiclesMetaPath,
            options.CarVariationsMetaPath,
            options.CarColsMetaPath,
            options.VehicleLayoutsMetaPath
        };

        foreach (var filePath in metaFiles)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                continue;

            File.Copy(filePath, Path.Combine(dataDir, Path.GetFileName(filePath)), overwrite: true);
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

        // CRITICAL: data_file order matters!
        // vehiclelayouts MUST be before vehicles.meta
        // vehicles.meta MUST be LAST

        if (!string.IsNullOrEmpty(options.HandlingMetaPath) && File.Exists(options.HandlingMetaPath))
            sb.AppendLine("data_file 'HANDLING_FILE'           'data/**/handling.meta'");

        if (!string.IsNullOrEmpty(options.CarColsMetaPath) && File.Exists(options.CarColsMetaPath))
            sb.AppendLine("data_file 'CARCOLS_FILE'            'data/**/carcols.meta'");

        if (!string.IsNullOrEmpty(options.CarVariationsMetaPath) && File.Exists(options.CarVariationsMetaPath))
            sb.AppendLine("data_file 'VEHICLE_VARIATION_FILE'  'data/**/carvariations.meta'");

        // vehiclelayouts BEFORE vehicles.meta!
        if (!string.IsNullOrEmpty(options.VehicleLayoutsMetaPath) && File.Exists(options.VehicleLayoutsMetaPath))
            sb.AppendLine("data_file 'VEHICLE_LAYOUTS_FILE'    'data/**/vehiclelayouts.meta'");

        // vehicles.meta ALWAYS last!
        if (!string.IsNullOrEmpty(options.VehiclesMetaPath) && File.Exists(options.VehiclesMetaPath))
            sb.AppendLine("data_file 'VEHICLE_METADATA_FILE'   'data/**/vehicles.meta'");

        // client_script for vehicle_names.lua (AddTextEntry must run client-side)
        if (options.IncludeVehicleNames && !string.IsNullOrWhiteSpace(options.VehicleDisplayName))
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
        sb.AppendLine($"AddTextEntry('{EscapeLua(options.VehicleModelName ?? "")}', '{EscapeLua(options.VehicleDisplayName ?? "")}')");

        File.WriteAllText(Path.Combine(outputDir, "vehicle_names.lua"), sb.ToString());
    }

    private static string EscapeLua(string value)
    {
        return value.Replace("'", "\\'").Replace("\n", "").Replace("\r", "");
    }
}

public class ExportOptions
{
    public string ResourceName { get; set; } = "";
    public string Author { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string OutputPath { get; set; } = "";
    public bool IncludeVehicleNames { get; set; }
    public string? VehicleModelName { get; set; }
    public string? VehicleDisplayName { get; set; }

    // Stream files
    public string? YftPath { get; set; }
    public string? YftHiPath { get; set; }
    public string? YtdPath { get; set; }
    public string? YtdHiPath { get; set; }

    // Meta files
    public string? HandlingMetaPath { get; set; }
    public string? VehiclesMetaPath { get; set; }
    public string? CarVariationsMetaPath { get; set; }
    public string? CarColsMetaPath { get; set; }
    public string? VehicleLayoutsMetaPath { get; set; }
}

public class ExportResult
{
    public bool Success { get; set; }
    public string OutputPath { get; set; } = "";
    public List<string> Warnings { get; } = new();
    public string? Error { get; set; }
}
