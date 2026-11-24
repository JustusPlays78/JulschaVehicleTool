using System.Xml.Linq;

namespace JulschaVehicleTool.Core.Services;

/// <summary>
/// Complete vehicles.meta &lt;Item&gt; for vehicles created in the tool (no imported XML).
/// Mirrors the structure of a vanilla car entry so that the game does not fall back to
/// zero/empty values for fields the tool does not expose (LODs, wheel scale, cameras, ...).
/// Fields the tool edits (modelName, txdName, flags, ...) are patched over this template.
/// </summary>
public static class VehicleMetaTemplate
{
    private const string Xml = """
        <Item>
          <modelName>template</modelName>
          <txdName>template</txdName>
          <handlingId>TEMPLATE</handlingId>
          <gameName>template</gameName>
          <vehicleMakeName />
          <expressionDictName>null</expressionDictName>
          <expressionName>null</expressionName>
          <animConvRoofDictName>null</animConvRoofDictName>
          <animConvRoofName>null</animConvRoofName>
          <animConvRoofWindowsAffected />
          <ptfxAssetName>null</ptfxAssetName>
          <audioNameHash />
          <layout>LAYOUT_STANDARD</layout>
          <coverBoundOffsets>STANDARD_COVER_OFFSET_INFO</coverBoundOffsets>
          <explosionInfo>EXPLOSION_INFO_DEFAULT</explosionInfo>
          <scenarioLayout />
          <cameraName>DEFAULT_FOLLOW_VEHICLE_CAMERA</cameraName>
          <aimCameraName>DEFAULT_THIRD_PERSON_VEHICLE_AIM_CAMERA</aimCameraName>
          <bonnetCameraName>VEHICLE_BONNET_CAMERA_STANDARD</bonnetCameraName>
          <povCameraName>DEFAULT_POV_CAMERA</povCameraName>
          <FirstPersonDriveByIKOffset x="0.000000" y="-0.060000" z="-0.065000" />
          <FirstPersonDriveByUnarmedIKOffset x="0.000000" y="-0.100000" z="0.000000" />
          <FirstPersonProjectileDriveByIKOffset x="0.000000" y="0.000000" z="0.000000" />
          <FirstPersonProjectileDriveByPassengerIKOffset x="0.000000" y="0.000000" z="0.000000" />
          <FirstPersonDriveByRightPassengerIKOffset x="0.000000" y="-0.060000" z="-0.065000" />
          <FirstPersonDriveByLeftPassengerIKOffset x="0.000000" y="0.000000" z="0.000000" />
          <FirstPersonDriveByRightPassengerUnarmedIKOffset x="0.000000" y="-0.100000" z="0.000000" />
          <FirstPersonDriveByLeftPassengerUnarmedIKOffset x="0.000000" y="0.000000" z="0.000000" />
          <FirstPersonMobilePhoneOffset x="0.155000" y="0.250000" z="0.520000" />
          <FirstPersonPassengerMobilePhoneOffset x="0.136000" y="0.223000" z="0.425000" />
          <PovCameraOffset x="0.000000" y="-0.150000" z="0.650000" />
          <PovCameraVerticalAdjustmentForRollCage value="0.000000" />
          <PovPassengerCameraOffset x="0.000000" y="0.000000" z="0.000000" />
          <PovRearPassengerCameraOffset x="0.000000" y="0.000000" z="0.000000" />
          <vfxInfoName>VFXVEHICLEINFO_CAR_GENERIC</vfxInfoName>
          <shouldUseCinematicViewMode value="true" />
          <shouldCameraTransitionOnClimbUpDown value="false" />
          <shouldCameraIgnoreExiting value="false" />
          <AllowPretendOccupants value="true" />
          <AllowJoyriding value="true" />
          <AllowSundayDriving value="true" />
          <AllowBodyColorMapping value="true" />
          <wheelScale value="0.250000" />
          <wheelScaleRear value="0.250000" />
          <dirtLevelMin value="0.000000" />
          <dirtLevelMax value="0.300000" />
          <envEffScaleMin value="0.000000" />
          <envEffScaleMax value="1.000000" />
          <envEffScaleMin2 value="0.000000" />
          <envEffScaleMax2 value="1.000000" />
          <damageMapScale value="0.600000" />
          <damageOffsetScale value="1.000000" />
          <diffuseTint value="0x00FFFFFF" />
          <steerWheelMult value="1.000000" />
          <HDTextureDist value="5.000000" />
          <lodDistances content="float_array">
            15.000000
            30.000000
            60.000000
            120.000000
            500.000000
            500.000000
          </lodDistances>
          <minSeatHeight value="0.850000" />
          <identicalModelSpawnDistance value="20" />
          <maxNumOfSameColor value="10" />
          <defaultBodyHealth value="1000.000000" />
          <pretendOccupantsScale value="1.000000" />
          <visibleSpawnDistScale value="1.000000" />
          <trackerPathWidth value="2.000000" />
          <weaponForceMult value="1.000000" />
          <frequency value="30" />
          <swankness>SWANKNESS_1</swankness>
          <maxNum value="10" />
          <flags />
          <type>VEHICLE_TYPE_CAR</type>
          <plateType>VPT_FRONT_AND_BACK_PLATES</plateType>
          <dashboardType>VDT_DEFAULT</dashboardType>
          <vehicleClass>VC_SEDAN</vehicleClass>
          <wheelType>VWT_SPORT</wheelType>
          <trailers />
          <additionalTrailers />
          <drivers />
          <extraIncludes />
          <doorsWithCollisionWhenClosed />
          <driveableDoors />
          <bumpersNeedToCollideWithMap value="false" />
          <needsRopeTexture value="false" />
          <requiredExtras />
          <rewards />
          <cinematicPartCamera>
            <Item>WHEEL_FRONT_RIGHT_CAMERA</Item>
            <Item>WHEEL_FRONT_LEFT_CAMERA</Item>
            <Item>WHEEL_REAR_RIGHT_CAMERA</Item>
            <Item>WHEEL_REAR_LEFT_CAMERA</Item>
          </cinematicPartCamera>
          <NmBraceOverrideSet />
          <buoyancySphereOffset x="0.000000" y="0.000000" z="0.000000" />
          <buoyancySphereSizeScale value="1.000000" />
          <pOverrideRagdollThreshold type="NULL" />
          <firstPersonDrivebyData />
        </Item>
        """;

    public static XElement Create() => XElement.Parse(Xml);
}
