using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace JulschaVehicleTool.Core.Models;

public partial class VehicleProject : ObservableObject
{
    [ObservableProperty]
    private string _projectName = "Untitled";

    [ObservableProperty]
    private string? _projectFilePath;

    // File references (paths to source files)
    [ObservableProperty]
    private string? _yftFilePath;

    [ObservableProperty]
    private string? _yftHiFilePath;

    [ObservableProperty]
    private string? _ytdFilePath;

    [ObservableProperty]
    private string? _ytdHiFilePath;

    [ObservableProperty]
    private string? _handlingMetaPath;

    [ObservableProperty]
    private string? _vehiclesMetaPath;

    [ObservableProperty]
    private string? _carVariationsMetaPath;

    [ObservableProperty]
    private string? _carColsMetaPath;

    [ObservableProperty]
    private string? _vehicleLayoutsMetaPath;

    [ObservableProperty]
    [JsonIgnore]
    private bool _isDirty;
}
