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

    /// <summary>
    /// Additional meta files exported unchanged with this resource (e.g. vehiclelayouts.meta).
    /// </summary>
    public ObservableCollection<PassthroughMeta> ExtraMetas { get; set; } = new();
}

/// <summary>A meta file the tool does not edit but exports as-is with a data_file entry.</summary>
public partial class PassthroughMeta : ObservableObject
{
    /// <summary>File name inside data/extra/ (e.g. vehiclelayouts_police.meta).</summary>
    [ObservableProperty] private string _fileName = "";
    /// <summary>FiveM data_file type (e.g. VEHICLE_LAYOUTS_FILE).</summary>
    [ObservableProperty] private string _dataFileType = "";
    [ObservableProperty] private string _content = "";
}
