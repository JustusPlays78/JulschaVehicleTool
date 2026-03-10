using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace JulschaVehicleTool.Core.Models;

public partial class Resource : ObservableObject
{
    [ObservableProperty]
    private string _name = "new_resource";

    [ObservableProperty]
    private string _author = "";

    [ObservableProperty]
    private string _version = "1.0.0";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private bool _includeVehicleNames = true;

    public ObservableCollection<Vehicle> Vehicles { get; set; } = new();
}
