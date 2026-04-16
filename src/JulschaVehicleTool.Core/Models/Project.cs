using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace JulschaVehicleTool.Core.Models;

public partial class Project : ObservableObject
{
    [ObservableProperty]
    private string _name = "Untitled";

    [ObservableProperty]
    private string _author = "";

    [ObservableProperty]
    private string _version = "1.0.0";

    [JsonIgnore]
    [ObservableProperty]
    private string? _folderPath;

    [JsonIgnore]
    [ObservableProperty]
    private bool _isDirty;

    public ObservableCollection<Resource> Resources { get; set; } = new();

    /// <summary>
    /// Project-level siren pool. All siren groups are defined here;
    /// vehicles reference them by ID via CarVariationData.SirenSettings.
    /// </summary>
    public CarColsData CarCols { get; } = new();
}
