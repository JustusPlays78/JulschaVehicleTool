using System.Numerics;

namespace JulschaVehicleTool.Core.Models;

/// <summary>
/// Factory for creating vehicles with sensible default values (standard sedan template).
/// </summary>
public static class VehicleDefaults
{
    /// <summary>
    /// Creates a new Vehicle with default handling, vehicle meta, car variation, and car cols data
    /// pre-filled with standard sedan values.
    /// </summary>
    public static Vehicle CreateDefault(string name = "new_vehicle")
    {
        return new Vehicle
        {
            Name = name,
            Handling = CreateDefaultHandling(name),
            VehicleMeta = CreateDefaultVehicleMeta(name),
            CarVariation = CreateDefaultCarVariation(name),
            CarCols = CreateDefaultCarCols(),
        };
    }

    public static HandlingData CreateDefaultHandling(string name) => new()
    {
        HandlingName = name.ToUpperInvariant(),
        FMass = 1500f,
        FInitialDragCoeff = 6.5f,
        FPercentSubmerged = 85f,
        VecCentreOfMassOffset = new Vector3(0f, 0f, -0.12f),
        VecInertiaMultiplier = new Vector3(1.2f, 1.1f, 1.4f),
        FDriveBiasFront = 0f,
        NInitialDriveGears = 6,
        FInitialDriveForce = 0.32f,
        FDriveInertia = 1.0f,
        FClutchChangeRateScaleUpShift = 2.5f,
        FClutchChangeRateScaleDownShift = 2.5f,
        FInitialDriveMaxFlatVel = 160f,
        FBrakeForce = 1.0f,
        FBrakeBiasFront = 0.65f,
        FHandBrakeForce = 0.8f,
        FSteeringLock = 40f,
        FTractionCurveMax = 2.5f,
        FTractionCurveMin = 2.0f,
        FTractionCurveLateral = 22.5f,
        FTractionSpringDeltaMax = 0.15f,
        FLowSpeedTractionLossMult = 1.0f,
        FTractionBiasFront = 0.48f,
        FTractionLossMult = 1.0f,
        FSuspensionForce = 2.0f,
        FSuspensionCompDamp = 1.5f,
        FSuspensionReboundDamp = 2.3f,
        FSuspensionUpperLimit = 0.12f,
        FSuspensionLowerLimit = -0.1f,
        FSuspensionBiasFront = 0.5f,
        FAntiRollBarForce = 0.8f,
        FAntiRollBarBiasFront = 0.65f,
        FRollCentreHeightFront = 0.36f,
        FRollCentreHeightRear = 0.36f,
        FCollisionDamageMult = 1.0f,
        FWeaponDamageMult = 1.0f,
        FDeformationDamageMult = 0.8f,
        FEngineDamageMult = 1.5f,
        FPetrolTankVolume = 65f,
        FOilVolume = 5f,
        NMonetaryValue = 25000,
        StrModelFlags = "440010",
        StrHandlingFlags = "0",
        StrDamageFlags = "0",
        AiHandling = "AVERAGE",
    };

    public static VehicleMetaData CreateDefaultVehicleMeta(string name) => new()
    {
        ModelName = name,
        TxdName = name,
        HandlingId = name.ToUpperInvariant(),
        GameName = name,
        VehicleMakeName = "",
        Type = "AUTOMOBILE",
        PlateType = "VPT_FRONT_AND_BACK_PLATES",
        DashboardType = "VDT_RACING",
        VehicleClass = "VC_SPORT",
        WheelType = "VWT_SPORT",
        Layout = "LAYOUT_STANDARD",
        Swankness = "SWANKNESS_NORMAL",
        MaxPassengers = 1,
        Mass = 1500f,
    };

    public static CarVariationData CreateDefaultCarVariation(string name) => new()
    {
        ModelName = name,
    };

    public static CarColsData CreateDefaultCarCols() => new();
}
