using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace JulschaVehicleTool.Core.Models;

public partial class Vehicle : ObservableObject
{
    [ObservableProperty]
    private string _name = "new_vehicle";

    // Relative paths within vehicles/<name>/ folder
    [ObservableProperty]
    private string? _yftRelativePath;

    [ObservableProperty]
    private string? _yftHiRelativePath;

    [ObservableProperty]
    private string? _ytdRelativePath;

    [ObservableProperty]
    private string? _ytdHiRelativePath;

    // Inline meta-data (stored in project.julveh JSON)
    [ObservableProperty]
    private HandlingData? _handling;

    [ObservableProperty]
    private CarVariationData? _carVariation;

    [ObservableProperty]
    private CarColsData? _carCols;

    [ObservableProperty]
    private VehicleMetaData? _vehicleMeta;

    /// <summary>
    /// Checks whether any referenced binary files are missing from the project folder.
    /// Must be called with the project's folder path to resolve relative paths.
    /// </summary>
    public bool HasMissingFiles(string? projectFolderPath)
    {
        if (string.IsNullOrEmpty(projectFolderPath))
            return true;

        var vehiclesDir = Path.Combine(projectFolderPath, "vehicles");

        if (YftRelativePath != null && !File.Exists(Path.Combine(vehiclesDir, YftRelativePath)))
            return true;
        if (YtdRelativePath != null && !File.Exists(Path.Combine(vehiclesDir, YtdRelativePath)))
            return true;
        if (YftHiRelativePath != null && !File.Exists(Path.Combine(vehiclesDir, YftHiRelativePath)))
            return true;
        if (YtdHiRelativePath != null && !File.Exists(Path.Combine(vehiclesDir, YtdHiRelativePath)))
            return true;

        return false;
    }
}
