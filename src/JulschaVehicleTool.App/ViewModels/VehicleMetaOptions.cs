namespace JulschaVehicleTool.App.ViewModels;

/// <summary>
/// Known GTA V vehicles.meta enum values for ComboBox suggestions.
/// All fields remain strings so custom values can still be typed.
/// </summary>
public static class VehicleMetaOptions
{
    public static readonly string[] Types =
    [
        "VEHICLE_TYPE_CAR", "VEHICLE_TYPE_BIKE", "VEHICLE_TYPE_BOAT",
        "VEHICLE_TYPE_HELI", "VEHICLE_TYPE_PLANE", "VEHICLE_TYPE_SUBMARINE",
        "VEHICLE_TYPE_TRAILER", "VEHICLE_TYPE_TRAIN", "VEHICLE_TYPE_BICYCLE",
        "VEHICLE_TYPE_QUADBIKE", "VEHICLE_TYPE_BLIMP",
        "VEHICLE_TYPE_AMPHIBIOUS_AUTOMOBILE", "VEHICLE_TYPE_AMPHIBIOUS_QUADBIKE",
        "VEHICLE_TYPE_SUBMARINECAR"
    ];

    public static readonly string[] VehicleClasses =
    [
        "VC_COMPACT", "VC_SEDAN", "VC_SUV", "VC_COUPE",
        "VC_SPORT_CLASSIC", "VC_SPORT", "VC_MUSCLE", "VC_SUPER", "VC_MOTORCYCLE",
        "VC_OFF_ROAD", "VC_INDUSTRIAL", "VC_UTILITY", "VC_VAN",
        "VC_BICYCLE", "VC_BOAT", "VC_HELICOPTER", "VC_PLANE",
        "VC_SERVICE", "VC_EMERGENCY", "VC_MILITARY", "VC_COMMERCIAL",
        "VC_RAIL", "VC_OPEN_WHEEL"
    ];

    public static readonly string[] WheelTypes =
    [
        "VWT_SPORT", "VWT_MUSCLE", "VWT_LOWRIDER", "VWT_SUV",
        "VWT_OFFROAD", "VWT_TUNER", "VWT_BIKE", "VWT_HIEND",
        "VWT_SUPERMOD1", "VWT_SUPERMOD2"
    ];

    public static readonly string[] PlateTypes =
    [
        "VPT_FRONT_AND_BACK_PLATES", "VPT_FRONT_PLATES",
        "VPT_BACK_PLATES", "VPT_NONE"
    ];

    public static readonly string[] DashboardTypes =
    [
        "VDT_DEFAULT", "VDT_STANDARD", "VDT_TRUCK", "VDT_MUSCLE",
        "VDT_MOTORBIKE", "VDT_RACING", "VDT_SPORT", "VDT_BOAT",
        "VDT_CADILLAC", "VDT_CAMPER", "VDT_FLASHGT", "VDT_GAUNTLET",
        "VDT_HOTRING", "VDT_POLICE", "VDT_RALLYTRUCK", "VDT_STOCKADE",
        "VDT_TRACTOR", "VDT_VOODOO", "VDT_WIRELESS"
    ];

    public static readonly string[] Layouts =
    [
        "LAYOUT_STANDARD", "LAYOUT_STANDARD_LOWRIDER", "LAYOUT_BIKE",
        "LAYOUT_LOW", "LAYOUT_TRIKE", "LAYOUT_QUAD", "LAYOUT_BOAT",
        "LAYOUT_HELICOPTER", "LAYOUT_PLANE", "LAYOUT_AMPHIBIOUS_CAR",
        "LAYOUT_SUBMARINE", "LAYOUT_TRAILER"
    ];

    public static readonly string[] Swanknesses =
    [
        "SWANKNESS_0", "SWANKNESS_1", "SWANKNESS_2",
        "SWANKNESS_3", "SWANKNESS_4", "SWANKNESS_5"
    ];
}
