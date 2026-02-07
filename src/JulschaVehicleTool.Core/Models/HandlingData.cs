using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Numerics;

namespace JulschaVehicleTool.Core.Models;

public partial class HandlingData : ObservableObject
{
    // Identity
    [ObservableProperty] private string _handlingName = "";

    // Physical
    [ObservableProperty] private float _fMass = 1500f;
    [ObservableProperty] private float _fInitialDragCoeff = 8.0f;
    [ObservableProperty] private float _fDownForceModifier = 1.0f;
    [ObservableProperty] private float _fPopUpLightRotation = 0f;
    [ObservableProperty] private float _fPercentSubmerged = 85f;
    [ObservableProperty] private Vector3 _vecCentreOfMassOffset;
    [ObservableProperty] private Vector3 _vecInertiaMultiplier = new(1f, 1f, 1f);

    // Transmission
    [ObservableProperty] private float _fDriveBiasFront = 0f;
    [ObservableProperty] private int _nInitialDriveGears = 6;
    [ObservableProperty] private float _fInitialDriveForce = 0.32f;
    [ObservableProperty] private float _fDriveInertia = 1.0f;
    [ObservableProperty] private float _fClutchChangeRateScaleUpShift = 2.5f;
    [ObservableProperty] private float _fClutchChangeRateScaleDownShift = 2.5f;
    [ObservableProperty] private float _fInitialDriveMaxFlatVel = 150f;

    // Braking
    [ObservableProperty] private float _fBrakeForce = 1.0f;
    [ObservableProperty] private float _fBrakeBiasFront = 0.65f;
    [ObservableProperty] private float _fHandBrakeForce = 0.8f;

    // Steering
    [ObservableProperty] private float _fSteeringLock = 40f;

    // Traction
    [ObservableProperty] private float _fTractionCurveMax = 2.5f;
    [ObservableProperty] private float _fTractionCurveMin = 2.0f;
    [ObservableProperty] private float _fTractionCurveLateral = 22.5f;
    [ObservableProperty] private float _fTractionSpringDeltaMax = 0.15f;
    [ObservableProperty] private float _fLowSpeedTractionLossMult = 1.0f;
    [ObservableProperty] private float _fCamberStiffnesss = 0f;
    [ObservableProperty] private float _fTractionBiasFront = 0.48f;
    [ObservableProperty] private float _fTractionLossMult = 1.0f;

    // Suspension
    [ObservableProperty] private float _fSuspensionForce = 2.0f;
    [ObservableProperty] private float _fSuspensionCompDamp = 1.5f;
    [ObservableProperty] private float _fSuspensionReboundDamp = 2.3f;
    [ObservableProperty] private float _fSuspensionUpperLimit = 0.12f;
    [ObservableProperty] private float _fSuspensionLowerLimit = -0.1f;
    [ObservableProperty] private float _fSuspensionRaise = 0f;
    [ObservableProperty] private float _fSuspensionBiasFront = 0.5f;
    [ObservableProperty] private float _fAntiRollBarForce = 0.8f;
    [ObservableProperty] private float _fAntiRollBarBiasFront = 0.65f;
    [ObservableProperty] private float _fRollCentreHeightFront = 0.36f;
    [ObservableProperty] private float _fRollCentreHeightRear = 0.36f;

    // Damage
    [ObservableProperty] private float _fCollisionDamageMult = 1.0f;
    [ObservableProperty] private float _fWeaponDamageMult = 1.0f;
    [ObservableProperty] private float _fDeformationDamageMult = 0.8f;
    [ObservableProperty] private float _fEngineDamageMult = 1.5f;
    [ObservableProperty] private float _fPetrolTankVolume = 65f;
    [ObservableProperty] private float _fOilVolume = 5f;

    // Misc
    [ObservableProperty] private float _fSeatOffsetDistX = 0f;
    [ObservableProperty] private float _fSeatOffsetDistY = 0f;
    [ObservableProperty] private float _fSeatOffsetDistZ = 0f;
    [ObservableProperty] private int _nMonetaryValue = 25000;
    [ObservableProperty] private string _strModelFlags = "440010";
    [ObservableProperty] private string _strHandlingFlags = "0";
    [ObservableProperty] private string _strDamageFlags = "0";
    [ObservableProperty] private string _aiHandling = "AVERAGE";

    // SubHandlingData
    public ObservableCollection<SubHandlingDataBase> SubHandlingItems { get; } = new();
}

public abstract class SubHandlingDataBase : ObservableObject
{
    public abstract string TypeName { get; }
}

public partial class CCarHandlingData : SubHandlingDataBase
{
    public override string TypeName => "CCarHandlingData";

    [ObservableProperty] private float _fBackEndPopUpCarImpulseMult = 0.05f;
    [ObservableProperty] private float _fBackEndPopUpBuildingImpulseMult = 0.03f;
    [ObservableProperty] private float _fBackEndPopUpMaxDeltaSpeed = 0.6f;
    [ObservableProperty] private float _fToeFront = 0f;
    [ObservableProperty] private float _fToeRear = 0f;
    [ObservableProperty] private float _fCamberFront = 0f;
    [ObservableProperty] private float _fCamberRear = 0f;
    [ObservableProperty] private float _fCastor = 0f;
    [ObservableProperty] private float _fEngineResistance = 0f;
    [ObservableProperty] private float _fMaxDriveBiasTransfer = 0f;
    [ObservableProperty] private float _fJumpForceScale = 0f;
    [ObservableProperty] private int _nAdvancedFlags = 0;
}

public partial class CBikeHandlingData : SubHandlingDataBase
{
    public override string TypeName => "CBikeHandlingData";

    [ObservableProperty] private float _fLeanFwdCOMMult = 0f;
    [ObservableProperty] private float _fLeanFwdForceMult = 0f;
    [ObservableProperty] private float _fLeanBakCOMMult = 0f;
    [ObservableProperty] private float _fLeanBakForceMult = 0f;
    [ObservableProperty] private float _fMaxBankAngle = 48f;
    [ObservableProperty] private float _fFullAnimAngle = 30f;
    [ObservableProperty] private float _fDesLeanReturnFrac = 0.1f;
    [ObservableProperty] private float _fStickLeanMult = 1.0f;
    [ObservableProperty] private float _fBrakingStabilityMult = 0f;
    [ObservableProperty] private float _fInAirSteerMult = 0f;
    [ObservableProperty] private float _fWheelieBalancePoint = -1f;
    [ObservableProperty] private float _fStoppieBalancePoint = -1f;
    [ObservableProperty] private float _fWheelieSteerMult = 0f;
    [ObservableProperty] private float _fRearBalanceMult = 1.0f;
    [ObservableProperty] private float _fFrontBalanceMult = 1.0f;
    [ObservableProperty] private float _fBikeGroundSideFrictionMult = 0f;
    [ObservableProperty] private float _fBikeWheelGroundSideFrictionMult = 0f;
    [ObservableProperty] private float _fBikeOnStandLeanAngle = 20f;
    [ObservableProperty] private float _fBikeOnStandSteerAngle = 30f;
    [ObservableProperty] private float _fJumpForce = 0f;
}

public partial class CBoatHandlingData : SubHandlingDataBase
{
    public override string TypeName => "CBoatHandlingData";

    [ObservableProperty] private float _fBoxFrontMult = 1f;
    [ObservableProperty] private float _fBoxRearMult = 1f;
    [ObservableProperty] private float _fBoxSideMult = 1f;
    [ObservableProperty] private float _fSampleTop = 0f;
    [ObservableProperty] private float _fSampleBottom = -1f;
    [ObservableProperty] private float _fAquaplaneForce = 0f;
    [ObservableProperty] private float _fAquaplanePushWaterMult = 1f;
    [ObservableProperty] private float _fAquaplanePushWaterCap = 1f;
    [ObservableProperty] private float _fAquaplanePushWaterApply = 1f;
    [ObservableProperty] private float _fRudderForce = 0f;
    [ObservableProperty] private float _fRudderOffsetSubmerge = 0f;
    [ObservableProperty] private float _fRudderOffsetForce = 0f;
    [ObservableProperty] private float _fRudderOffsetForceZMult = 0f;
    [ObservableProperty] private float _fWaveAudioMult = 1f;
    [ObservableProperty] private float _fLook_L_R_CamHeight = 0.6f;
    [ObservableProperty] private float _fDragCoefficient = 0f;
    [ObservableProperty] private float _fKeelSphereSize = 0f;
    [ObservableProperty] private float _fPropRadius = 0f;
    [ObservableProperty] private float _fLowLodAngOffset = 0f;
    [ObservableProperty] private float _fLowLodDraughtOffset = 0f;
    [ObservableProperty] private float _fImpellerOffset = -1f;
    [ObservableProperty] private float _fImpellerForceMult = 0f;
    [ObservableProperty] private float _fDinghySphereBuoyConst = 0f;
    [ObservableProperty] private float _fProwRaiseMult = 0f;
    [ObservableProperty] private float _fDeepSurfaceImpulseMult = 0f;
    [ObservableProperty] private float _fDeepSurfaceFrictionMult = 0f;
    [ObservableProperty] private float _fWindMult = 0f;
}

public partial class CFlyingHandlingData : SubHandlingDataBase
{
    public override string TypeName => "CFlyingHandlingData";

    [ObservableProperty] private float _fThrust = 0f;
    [ObservableProperty] private float _fThrustFallOff = 0f;
    [ObservableProperty] private float _fThrustVectoring = 0f;
    [ObservableProperty] private float _fYawMult = 0f;
    [ObservableProperty] private float _fYawStabilise = 0f;
    [ObservableProperty] private float _fSideSlipMult = 0f;
    [ObservableProperty] private float _fRollMult = 0f;
    [ObservableProperty] private float _fRollStabilise = 0f;
    [ObservableProperty] private float _fPitchMult = 0f;
    [ObservableProperty] private float _fPitchStabilise = 0f;
    [ObservableProperty] private float _fFormLiftMult = 0f;
    [ObservableProperty] private float _fAttackLiftMult = 0f;
    [ObservableProperty] private float _fAttackDiveMult = 0f;
    [ObservableProperty] private float _fGearDownDragV = 0f;
    [ObservableProperty] private float _fGearDownLiftMult = 0f;
    [ObservableProperty] private float _fWindMult = 0f;
    [ObservableProperty] private float _fMoveRes = 0f;
    [ObservableProperty] private Vector3 _vecTurnRes;
    [ObservableProperty] private Vector3 _vecSpeedRes;
    [ObservableProperty] private float _fGearDoorFrontOpen = 0f;
    [ObservableProperty] private float _fGearDoorRearOpen = 0f;
    [ObservableProperty] private float _fGearDoorRearOpen2 = 0f;
    [ObservableProperty] private float _fGearDoorRearMOpen = 0f;
    [ObservableProperty] private float _fTurboThrust = 0f;
    [ObservableProperty] private float _fPitchTurboAdjust = 0f;
    [ObservableProperty] private float _fHoverVelMult = 0f;
    [ObservableProperty] private string _handlingType = "YOURTYPE";
}
