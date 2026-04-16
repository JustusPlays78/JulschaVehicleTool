namespace JulschaVehicleTool.App.ViewModels;

/// <summary>
/// Known GTA V vehicles.meta enum values for ComboBox suggestions.
/// All fields remain strings so custom values can still be typed.
/// </summary>
public static class VehicleMetaOptions
{
    public static readonly string[] Types =
    [
        "AUTOMOBILE", "BIKE", "BOAT", "HELICOPTER", "PLANE",
        "SUBMARINE", "TRAILER", "TRAIN", "AMPHIBIOUS_AUTOMOBILE",
        "AMPHIBIOUS_QUADBIKE", "QUADBIKE", "BLIMP", "SUBMARINE_CAR"
    ];

    public static readonly string[] VehicleClasses =
    [
        "VC_SPORT", "VC_MUSCLE", "VC_SUPER", "VC_MOTORCYCLE",
        "VC_OFF_ROAD", "VC_INDUSTRIAL", "VC_UTILITY", "VC_VAN",
        "VC_BICYCLE", "VC_BOAT", "VC_HELICOPTER", "VC_PLANE",
        "VC_SERVICE", "VC_EMERGENCY", "VC_MILITARY", "VC_COMMERCIAL",
        "VC_RAIL", "VC_OPEN_WHEEL"
    ];

    public static readonly string[] WheelTypes =
    [
        "VWT_SPORT", "VWT_MUSCLE", "VWT_LOWRIDER", "VWT_SUV",
        "VWT_OFFROAD", "VWT_TUNER", "VWT_BIKE_WHEELS", "VWT_HIGH_END",
        "VWT_BENNY_ORIGINAL", "VWT_BENNY_BESPOKE", "VWT_OPEN_WHEEL"
    ];

    public static readonly string[] PlateTypes =
    [
        "VPT_FRONT_AND_BACK_PLATES", "VPT_FRONT_PLATE",
        "VPT_BACK_PLATE", "VPT_NONE"
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
        "SWANKNESS_LOWEST", "SWANKNESS_LOW", "SWANKNESS_NORMAL",
        "SWANKNESS_HIGH", "SWANKNESS_HIGHEST"
    ];
}
