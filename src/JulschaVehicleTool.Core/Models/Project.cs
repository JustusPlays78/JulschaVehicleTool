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

    /// <summary>
    /// Encrypt the project file and vehicle files with DPAPI (bound to this Windows user).
    /// Off by default: an encrypted project can only be opened by the same user on the same PC.
    /// Change it with IProjectService.SetLocalEncryption so existing files are converted.
    /// </summary>
    [ObservableProperty]
    private bool _encryptLocal;

    /// <summary>Set when the project file was unreadable and the backup (project.julveh.bak) was opened.</summary>
    [JsonIgnore]
    public bool LoadedFromBackup { get; set; }

    /// <summary>
    /// Siren setting IDs already used by other resources on the server (e.g. "1-50, 99").
    /// The tool never assigns these to new or imported siren settings.
    /// </summary>
    [ObservableProperty]
    private string _reservedSirenIds = "";

    public ObservableCollection<Resource> Resources { get; set; } = new();

    /// <summary>
    /// Project-level siren pool. All siren groups are defined here;
    /// vehicles reference them by ID via CarVariationData.SirenSettings.
    /// </summary>
    public CarColsData CarCols { get; } = new();
}
