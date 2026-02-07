using CommunityToolkit.Mvvm.ComponentModel;

namespace JulschaVehicleTool.Core.Models;

public partial class VehicleMetaData : ObservableObject
{
    [ObservableProperty] private string _modelName = "";
    [ObservableProperty] private string _txdName = "";
    [ObservableProperty] private string _handlingId = "";
    [ObservableProperty] private string _gameName = "";
    [ObservableProperty] private string _vehicleMakeName = "";
    [ObservableProperty] private string _type = "AUTOMOBILE";
    [ObservableProperty] private string _plateType = "VPT_FRONT_AND_BACK_PLATES";
    [ObservableProperty] private string _dashboardType = "VDT_RACING";
    [ObservableProperty] private string _vehicleClass = "VC_SPORT";
    [ObservableProperty] private string _wheelType = "VWT_SPORT";
    [ObservableProperty] private int _diffuseColor = 0;
    [ObservableProperty] private string _layout = "LAYOUT_STANDARD";

    // Flags
    [ObservableProperty] private string _flags = "";
    [ObservableProperty] private string _strFlags = "";

    // Audio
    [ObservableProperty] private string _audioNameHash = "";

    // Swankness
    [ObservableProperty] private string _swankness = "SWANKNESS_NORMAL";

    // Limits
    [ObservableProperty] private int _maxPassengers = 1;
    [ObservableProperty] private float _mass = 1500f;
    [ObservableProperty] private float _percentSubmergedLevel;
    [ObservableProperty] private float _prevehicleConvRoofDismount;
    [ObservableProperty] private float _searchLight;

    // Extras
    [ObservableProperty] private string _rewards = "";
    [ObservableProperty] private string _cinematicPartCamera = "";
}
