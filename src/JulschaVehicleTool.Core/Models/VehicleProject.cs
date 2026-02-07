using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace JulschaVehicleTool.Core.Models;

public partial class VehicleProject : ObservableObject
{
    [ObservableProperty]
    private string _projectName = "Untitled";

    [ObservableProperty]
    private string? _projectFilePath;

    public ObservableCollection<VehicleEntry> Vehicles { get; set; } = new();

    [ObservableProperty]
    [JsonIgnore]
    private bool _isDirty;
}
