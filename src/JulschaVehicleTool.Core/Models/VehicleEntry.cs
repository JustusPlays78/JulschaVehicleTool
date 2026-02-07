using CommunityToolkit.Mvvm.ComponentModel;

namespace JulschaVehicleTool.Core.Models;

public partial class VehicleEntry : ObservableObject
{
    [ObservableProperty] private string _name = "New Vehicle";

    // Stream files
    [ObservableProperty] private string? _yftFilePath;
    [ObservableProperty] private string? _yftHiFilePath;
    [ObservableProperty] private string? _ytdFilePath;
    [ObservableProperty] private string? _ytdHiFilePath;

    // Meta files
    [ObservableProperty] private string? _handlingMetaPath;
    [ObservableProperty] private string? _vehiclesMetaPath;
    [ObservableProperty] private string? _carVariationsMetaPath;
    [ObservableProperty] private string? _carColsMetaPath;
    [ObservableProperty] private string? _vehicleLayoutsMetaPath;
}
