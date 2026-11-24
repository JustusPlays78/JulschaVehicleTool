using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace JulschaVehicleTool.Core.Models;

public partial class VehicleMetaData : ObservableObject
{
    [ObservableProperty] private string _modelName = "";

    /// <summary>
    /// Original &lt;Item&gt; XML this entry was imported from (null for entries created in the tool).
    /// On export only changed fields are patched into it; everything else is preserved 1:1.
    /// </summary>
    [ObservableProperty] private string? _sourceXml;

    /// <summary>
    /// Text shown in game for <see cref="GameName"/> (written to vehicle_names.lua via AddTextEntry).
    /// Not part of vehicles.meta.
    /// </summary>
    [ObservableProperty] private string _displayName = "";
    [ObservableProperty] private string _txdName = "";
    [ObservableProperty] private string _handlingId = "";
    [ObservableProperty] private string _gameName = "";
    [ObservableProperty] private string _vehicleMakeName = "";
    [ObservableProperty] private string _type = "VEHICLE_TYPE_CAR";
    [ObservableProperty] private string _plateType = "VPT_FRONT_AND_BACK_PLATES";
    [ObservableProperty] private string _dashboardType = "VDT_RACING";
    [ObservableProperty] private string _vehicleClass = "VC_SPORT";
    [ObservableProperty] private string _wheelType = "VWT_SPORT";
    [ObservableProperty] private int _diffuseColor = 0;
    [ObservableProperty] private string _layout = "LAYOUT_STANDARD";

    // NOTE: the following fields are kept for UI compatibility but are not part of the
    // vanilla vehicles.meta schema. They are only written back if the imported XML
    // already contains them: maxPassengers, mass, percentSubmerged,
    // prevehicleConvRoofDismount, searchLight, diffuseColor, strFlags.

    // Flags
    [ObservableProperty] private string _flags = "";
    [ObservableProperty] private string _strFlags = "";

    // Audio
    [ObservableProperty] private string _audioNameHash = "";

    // Swankness
    [ObservableProperty] private string _swankness = "SWANKNESS_1";

    // Limits
    [ObservableProperty] private int _maxPassengers = 1;
    [ObservableProperty] private float _mass = 1500f;
    [ObservableProperty] private float _percentSubmergedLevel;
    [ObservableProperty] private float _prevehicleConvRoofDismount;
    [ObservableProperty] private float _searchLight;

    // Extras
    [ObservableProperty] private string _rewards = "";
    [ObservableProperty] private string _cinematicPartCamera = "";

    /// <summary>
    /// txdRelationships entries (parent → child) needed by this vehicle, e.g. vehshare → modelName.
    /// </summary>
    public ObservableCollection<TxdRelationship> TxdRelationships { get; } = new();
}

public partial class TxdRelationship : ObservableObject
{
    [ObservableProperty] private string _parent = "";
    [ObservableProperty] private string _child = "";
}
