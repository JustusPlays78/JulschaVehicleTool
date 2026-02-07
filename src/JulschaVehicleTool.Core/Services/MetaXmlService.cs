using System.Globalization;
using System.Numerics;
using System.Xml.Linq;
using JulschaVehicleTool.Core.Models;

namespace JulschaVehicleTool.Core.Services;

public class MetaXmlService
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    #region handling.meta

    public HandlingData LoadHandling(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var item = doc.Descendants("Item").First();
        return ParseHandlingItem(item);
    }

    public HandlingData ParseHandlingItem(XElement item)
    {
        var data = new HandlingData
        {
            HandlingName = GetStr(item, "handlingName"),
            FMass = GetFloat(item, "fMass"),
            FInitialDragCoeff = GetFloat(item, "fInitialDragCoeff"),
            FDownForceModifier = GetFloat(item, "fDownForceModifier"),
            FPopUpLightRotation = GetFloat(item, "fPopUpLightRotation"),
            FPercentSubmerged = GetFloat(item, "fPercentSubmerged"),
            VecCentreOfMassOffset = GetVec3(item, "vecCentreOfMassOffset"),
            VecInertiaMultiplier = GetVec3(item, "vecInertiaMultiplier"),

            FDriveBiasFront = GetFloat(item, "fDriveBiasFront"),
            NInitialDriveGears = GetInt(item, "nInitialDriveGears"),
            FInitialDriveForce = GetFloat(item, "fInitialDriveForce"),
            FDriveInertia = GetFloat(item, "fDriveInertia"),
            FClutchChangeRateScaleUpShift = GetFloat(item, "fClutchChangeRateScaleUpShift"),
            FClutchChangeRateScaleDownShift = GetFloat(item, "fClutchChangeRateScaleDownShift"),
            FInitialDriveMaxFlatVel = GetFloat(item, "fInitialDriveMaxFlatVel"),

            FBrakeForce = GetFloat(item, "fBrakeForce"),
            FBrakeBiasFront = GetFloat(item, "fBrakeBiasFront"),
            FHandBrakeForce = GetFloat(item, "fHandBrakeForce"),

            FSteeringLock = GetFloat(item, "fSteeringLock"),

            FTractionCurveMax = GetFloat(item, "fTractionCurveMax"),
            FTractionCurveMin = GetFloat(item, "fTractionCurveMin"),
            FTractionCurveLateral = GetFloat(item, "fTractionCurveLateral"),
            FTractionSpringDeltaMax = GetFloat(item, "fTractionSpringDeltaMax"),
            FLowSpeedTractionLossMult = GetFloat(item, "fLowSpeedTractionLossMult"),
            FCamberStiffnesss = GetFloat(item, "fCamberStiffnesss"),
            FTractionBiasFront = GetFloat(item, "fTractionBiasFront"),
            FTractionLossMult = GetFloat(item, "fTractionLossMult"),

            FSuspensionForce = GetFloat(item, "fSuspensionForce"),
            FSuspensionCompDamp = GetFloat(item, "fSuspensionCompDamp"),
            FSuspensionReboundDamp = GetFloat(item, "fSuspensionReboundDamp"),
            FSuspensionUpperLimit = GetFloat(item, "fSuspensionUpperLimit"),
            FSuspensionLowerLimit = GetFloat(item, "fSuspensionLowerLimit"),
            FSuspensionRaise = GetFloat(item, "fSuspensionRaise"),
            FSuspensionBiasFront = GetFloat(item, "fSuspensionBiasFront"),
            FAntiRollBarForce = GetFloat(item, "fAntiRollBarForce"),
            FAntiRollBarBiasFront = GetFloat(item, "fAntiRollBarBiasFront"),
            FRollCentreHeightFront = GetFloat(item, "fRollCentreHeightFront"),
            FRollCentreHeightRear = GetFloat(item, "fRollCentreHeightRear"),

            FCollisionDamageMult = GetFloat(item, "fCollisionDamageMult"),
            FWeaponDamageMult = GetFloat(item, "fWeaponDamageMult"),
            FDeformationDamageMult = GetFloat(item, "fDeformationDamageMult"),
            FEngineDamageMult = GetFloat(item, "fEngineDamageMult"),
            FPetrolTankVolume = GetFloat(item, "fPetrolTankVolume"),
            FOilVolume = GetFloat(item, "fOilVolume"),

            FSeatOffsetDistX = GetFloat(item, "fSeatOffsetDistX"),
            FSeatOffsetDistY = GetFloat(item, "fSeatOffsetDistY"),
            FSeatOffsetDistZ = GetFloat(item, "fSeatOffsetDistZ"),
            NMonetaryValue = GetInt(item, "nMonetaryValue"),
            StrModelFlags = GetStr(item, "strModelFlags"),
            StrHandlingFlags = GetStr(item, "strHandlingFlags"),
            StrDamageFlags = GetStr(item, "strDamageFlags"),
            AiHandling = GetStr(item, "AIHandling"),
        };

        var subHandling = item.Element("SubHandlingData");
        if (subHandling != null)
        {
            foreach (var sub in subHandling.Elements("Item"))
            {
                var type = sub.Attribute("type")?.Value;
                SubHandlingDataBase? parsed = type switch
                {
                    "CCarHandlingData" => ParseCCarHandling(sub),
                    "CBikeHandlingData" => ParseCBikeHandling(sub),
                    "CBoatHandlingData" => ParseCBoatHandling(sub),
                    "CFlyingHandlingData" => ParseCFlyingHandling(sub),
                    _ => null
                };
                if (parsed != null) data.SubHandlingItems.Add(parsed);
            }
        }

        return data;
    }

    public void SaveHandling(HandlingData data, string filePath)
    {
        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null),
            new XElement("CHandlingDataMgr",
                new XElement("HandlingData",
                    SerializeHandlingItem(data)
                )
            )
        );
        doc.Save(filePath);
    }

    private XElement SerializeHandlingItem(HandlingData d)
    {
        var item = new XElement("Item", new XAttribute("type", "CHandlingData"),
            ValEl("handlingName", d.HandlingName),
            FloatEl("fMass", d.FMass),
            FloatEl("fInitialDragCoeff", d.FInitialDragCoeff),
            FloatEl("fDownForceModifier", d.FDownForceModifier),
            FloatEl("fPopUpLightRotation", d.FPopUpLightRotation),
            FloatEl("fPercentSubmerged", d.FPercentSubmerged),
            Vec3El("vecCentreOfMassOffset", d.VecCentreOfMassOffset),
            Vec3El("vecInertiaMultiplier", d.VecInertiaMultiplier),

            FloatEl("fDriveBiasFront", d.FDriveBiasFront),
            IntEl("nInitialDriveGears", d.NInitialDriveGears),
            FloatEl("fInitialDriveForce", d.FInitialDriveForce),
            FloatEl("fDriveInertia", d.FDriveInertia),
            FloatEl("fClutchChangeRateScaleUpShift", d.FClutchChangeRateScaleUpShift),
            FloatEl("fClutchChangeRateScaleDownShift", d.FClutchChangeRateScaleDownShift),
            FloatEl("fInitialDriveMaxFlatVel", d.FInitialDriveMaxFlatVel),

            FloatEl("fBrakeForce", d.FBrakeForce),
            FloatEl("fBrakeBiasFront", d.FBrakeBiasFront),
            FloatEl("fHandBrakeForce", d.FHandBrakeForce),

            FloatEl("fSteeringLock", d.FSteeringLock),

            FloatEl("fTractionCurveMax", d.FTractionCurveMax),
            FloatEl("fTractionCurveMin", d.FTractionCurveMin),
            FloatEl("fTractionCurveLateral", d.FTractionCurveLateral),
            FloatEl("fTractionSpringDeltaMax", d.FTractionSpringDeltaMax),
            FloatEl("fLowSpeedTractionLossMult", d.FLowSpeedTractionLossMult),
            FloatEl("fCamberStiffnesss", d.FCamberStiffnesss),
            FloatEl("fTractionBiasFront", d.FTractionBiasFront),
            FloatEl("fTractionLossMult", d.FTractionLossMult),

            FloatEl("fSuspensionForce", d.FSuspensionForce),
            FloatEl("fSuspensionCompDamp", d.FSuspensionCompDamp),
            FloatEl("fSuspensionReboundDamp", d.FSuspensionReboundDamp),
            FloatEl("fSuspensionUpperLimit", d.FSuspensionUpperLimit),
            FloatEl("fSuspensionLowerLimit", d.FSuspensionLowerLimit),
            FloatEl("fSuspensionRaise", d.FSuspensionRaise),
            FloatEl("fSuspensionBiasFront", d.FSuspensionBiasFront),
            FloatEl("fAntiRollBarForce", d.FAntiRollBarForce),
            FloatEl("fAntiRollBarBiasFront", d.FAntiRollBarBiasFront),
            FloatEl("fRollCentreHeightFront", d.FRollCentreHeightFront),
            FloatEl("fRollCentreHeightRear", d.FRollCentreHeightRear),

            FloatEl("fCollisionDamageMult", d.FCollisionDamageMult),
            FloatEl("fWeaponDamageMult", d.FWeaponDamageMult),
            FloatEl("fDeformationDamageMult", d.FDeformationDamageMult),
            FloatEl("fEngineDamageMult", d.FEngineDamageMult),
            FloatEl("fPetrolTankVolume", d.FPetrolTankVolume),
            FloatEl("fOilVolume", d.FOilVolume),

            FloatEl("fSeatOffsetDistX", d.FSeatOffsetDistX),
            FloatEl("fSeatOffsetDistY", d.FSeatOffsetDistY),
            FloatEl("fSeatOffsetDistZ", d.FSeatOffsetDistZ),
            IntEl("nMonetaryValue", d.NMonetaryValue),
            ValEl("strModelFlags", d.StrModelFlags),
            ValEl("strHandlingFlags", d.StrHandlingFlags),
            ValEl("strDamageFlags", d.StrDamageFlags),
            ValEl("AIHandling", d.AiHandling),
            SerializeSubHandling(d.SubHandlingItems)
        );
        return item;
    }

    #endregion

    #region carvariations.meta

    public CarVariationData LoadCarVariations(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var item = doc.Descendants("variationData").First().Element("Item");
        if (item == null) throw new InvalidOperationException("No vehicle variation data found");

        var data = new CarVariationData
        {
            ModelName = item.Element("modelName")?.Value ?? "",
            LightSettings = GetInt(item, "lightSettings"),
            SirenSettings = GetInt(item, "sirenSettings"),
        };

        var colors = item.Element("colors");
        if (colors != null)
        {
            foreach (var colorItem in colors.Elements("Item"))
            {
                var combo = new ColorCombination();
                var indices = colorItem.Element("indices")?.Value?.Trim().Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (indices is { Length: >= 1 }) combo.PrimaryColor = int.Parse(indices[0], Inv);
                if (indices is { Length: >= 2 }) combo.SecondaryColor = int.Parse(indices[1], Inv);
                if (indices is { Length: >= 3 }) combo.PearlescentColor = int.Parse(indices[2], Inv);
                if (indices is { Length: >= 4 }) combo.RimColor = int.Parse(indices[3], Inv);
                if (indices is { Length: >= 5 }) combo.InteriorTrimColor = int.Parse(indices[4], Inv);
                if (indices is { Length: >= 6 }) combo.DashboardColor = int.Parse(indices[5], Inv);

                var liveries = colorItem.Element("liveries");
                if (liveries != null)
                {
                    foreach (var liv in liveries.Elements("Item"))
                    {
                        var val = liv.Attribute("value")?.Value;
                        combo.Liveries.Add(val == "true");
                    }
                }

                data.Colors.Add(combo);
            }
        }

        var kits = item.Element("kits");
        if (kits != null)
            foreach (var kit in kits.Elements("Item"))
                data.Kits.Add(kit.Value?.Trim() ?? "");

        var plates = item.Element("plateProbabilities")?.Element("Probabilities");
        if (plates != null)
        {
            foreach (var plate in plates.Elements("Item"))
            {
                data.PlateProbabilities.Add(new PlateProbability
                {
                    Name = plate.Element("Name")?.Value ?? "",
                    Value = int.TryParse(plate.Element("Value")?.Attribute("value")?.Value, out var v) ? v : 100
                });
            }
        }

        return data;
    }

    public void SaveCarVariations(CarVariationData data, string filePath)
    {
        var colorsEl = new XElement("colors");
        foreach (var c in data.Colors)
        {
            var indices = $"\n              {c.PrimaryColor} {c.SecondaryColor} {c.PearlescentColor} {c.RimColor} {c.InteriorTrimColor} {c.DashboardColor}\n            ";
            var liveriesEl = new XElement("liveries");
            foreach (var liv in c.Liveries)
                liveriesEl.Add(new XElement("Item", new XAttribute("value", liv ? "true" : "false")));

            colorsEl.Add(new XElement("Item",
                new XElement("indices", new XAttribute("content", "char_array"), indices),
                liveriesEl
            ));
        }

        var kitsEl = new XElement("kits");
        foreach (var kit in data.Kits)
            kitsEl.Add(new XElement("Item", kit));

        var platesEl = new XElement("plateProbabilities", new XElement("Probabilities"));
        foreach (var plate in data.PlateProbabilities)
        {
            platesEl.Element("Probabilities")!.Add(new XElement("Item",
                new XElement("Name", plate.Name),
                new XElement("Value", new XAttribute("value", plate.Value))
            ));
        }

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null),
            new XElement("CVehicleModelInfoVariation",
                new XElement("variationData",
                    new XElement("Item",
                        new XElement("modelName", data.ModelName),
                        colorsEl,
                        kitsEl,
                        new XElement("windowsWithExposedEdges"),
                        platesEl,
                        new XElement("lightSettings", new XAttribute("value", data.LightSettings)),
                        new XElement("sirenSettings", new XAttribute("value", data.SirenSettings))
                    )
                )
            )
        );
        doc.Save(filePath);
    }

    #endregion

    #region carcols.meta (Sirens)

    public CarColsData LoadCarCols(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var data = new CarColsData();

        var sirens = doc.Descendants("Sirens").FirstOrDefault();
        if (sirens != null)
        {
            foreach (var sirenItem in sirens.Elements("Item"))
            {
                var setting = new SirenSetting
                {
                    Id = GetInt(sirenItem, "id"),
                    Name = sirenItem.Element("name")?.Value ?? "",
                    TimeMultiplier = GetFloat(sirenItem, "timeMultiplier"),
                    LightFalloffMax = GetFloat(sirenItem, "lightFalloffMax"),
                    LightFalloffExponent = GetFloat(sirenItem, "lightFalloffExponent"),
                    LightInnerConeAngle = GetFloat(sirenItem, "lightInnerConeAngle"),
                    LightOuterConeAngle = GetFloat(sirenItem, "lightOuterConeAngle"),
                    LightOffset = GetFloat(sirenItem, "lightOffset"),
                    TextureName = sirenItem.Element("textureName")?.Value ?? "VehicleLight_sirenlight",
                    SequencerBpm = GetUInt(sirenItem, "sequencerBpm"),
                    UseRealLights = GetBool(sirenItem, "useRealLights"),
                    LeftHeadLightSequencer = GetUInt(sirenItem.Element("leftHeadLight"), "sequencer"),
                    RightHeadLightSequencer = GetUInt(sirenItem.Element("rightHeadLight"), "sequencer"),
                    LeftTailLightSequencer = GetUInt(sirenItem.Element("leftTailLight"), "sequencer"),
                    RightTailLightSequencer = GetUInt(sirenItem.Element("rightTailLight"), "sequencer"),
                    LeftHeadLightMultiples = GetInt(sirenItem, "leftHeadLightMultiples"),
                    RightHeadLightMultiples = GetInt(sirenItem, "rightHeadLightMultiples"),
                    LeftTailLightMultiples = GetInt(sirenItem, "leftTailLightMultiples"),
                    RightTailLightMultiples = GetInt(sirenItem, "rightTailLightMultiples"),
                };

                var sirensList = sirenItem.Element("sirens");
                if (sirensList != null)
                {
                    foreach (var lightItem in sirensList.Elements("Item"))
                    {
                        setting.Sirens.Add(ParseSirenLight(lightItem));
                    }
                }

                data.SirenSettings.Add(setting);
            }
        }

        return data;
    }

    private SirenLight ParseSirenLight(XElement el)
    {
        var rot = el.Element("rotation");
        var flash = el.Element("flashiness");
        var corona = el.Element("corona");

        return new SirenLight
        {
            RotationDelta = GetFloat(rot, "delta"),
            RotationStart = GetFloat(rot, "start"),
            RotationSpeed = GetFloat(rot, "speed"),
            RotationSequencer = GetUInt(rot, "sequencer"),
            RotationMultiples = GetInt(rot, "multiples"),
            RotationDirection = GetBool(rot, "direction"),
            RotationSyncToBpm = GetBool(rot, "syncToBpm"),

            FlashinessDelta = GetFloat(flash, "delta"),
            FlashinessStart = GetFloat(flash, "start"),
            FlashinessSpeed = GetFloat(flash, "speed"),
            FlashinessSequencer = GetUInt(flash, "sequencer"),
            FlashinessMultiples = GetInt(flash, "multiples"),
            FlashinessDirection = GetBool(flash, "direction"),
            FlashinessSyncToBpm = GetBool(flash, "syncToBpm"),

            CoronaIntensity = GetFloat(corona, "intensity"),
            CoronaSize = GetFloat(corona, "size"),
            CoronaPull = GetFloat(corona, "pull"),
            CoronaFaceCamera = GetBool(corona, "faceCamera"),

            Color = el.Element("color")?.Attribute("value")?.Value ?? "0xFFFF0A0A",
            Intensity = GetFloat(el, "intensity"),
            LightGroup = GetInt(el, "lightGroup"),
            Rotate = GetBool(el, "rotate"),
            Scale = GetBool(el, "scale"),
            ScaleFactor = GetFloat(el, "scaleFactor"),
            Flash = GetBool(el, "flash"),
            Light = GetBool(el, "light"),
            SpotLight = GetBool(el, "spotLight"),
            CastShadows = GetBool(el, "castShadows"),
        };
    }

    public void SaveCarCols(CarColsData data, string filePath)
    {
        var sirensEl = new XElement("Sirens");
        foreach (var s in data.SirenSettings)
        {
            var sirenListEl = new XElement("sirens");
            foreach (var light in s.Sirens)
            {
                sirenListEl.Add(new XElement("Item",
                    new XElement("rotation",
                        FloatEl("delta", light.RotationDelta),
                        FloatEl("start", light.RotationStart),
                        FloatEl("speed", light.RotationSpeed),
                        UIntEl("sequencer", light.RotationSequencer),
                        IntEl("multiples", light.RotationMultiples),
                        BoolEl("direction", light.RotationDirection),
                        BoolEl("syncToBpm", light.RotationSyncToBpm)
                    ),
                    new XElement("flashiness",
                        FloatEl("delta", light.FlashinessDelta),
                        FloatEl("start", light.FlashinessStart),
                        FloatEl("speed", light.FlashinessSpeed),
                        UIntEl("sequencer", light.FlashinessSequencer),
                        IntEl("multiples", light.FlashinessMultiples),
                        BoolEl("direction", light.FlashinessDirection),
                        BoolEl("syncToBpm", light.FlashinessSyncToBpm)
                    ),
                    new XElement("corona",
                        FloatEl("intensity", light.CoronaIntensity),
                        FloatEl("size", light.CoronaSize),
                        FloatEl("pull", light.CoronaPull),
                        BoolEl("faceCamera", light.CoronaFaceCamera)
                    ),
                    new XElement("color", new XAttribute("value", light.Color)),
                    FloatEl("intensity", light.Intensity),
                    IntEl("lightGroup", light.LightGroup),
                    BoolEl("rotate", light.Rotate),
                    BoolEl("scale", light.Scale),
                    FloatEl("scaleFactor", light.ScaleFactor),
                    BoolEl("flash", light.Flash),
                    BoolEl("light", light.Light),
                    BoolEl("spotLight", light.SpotLight),
                    BoolEl("castShadows", light.CastShadows)
                ));
            }

            sirensEl.Add(new XElement("Item",
                IntEl("id", s.Id),
                new XElement("name", s.Name),
                FloatEl("timeMultiplier", s.TimeMultiplier),
                FloatEl("lightFalloffMax", s.LightFalloffMax),
                FloatEl("lightFalloffExponent", s.LightFalloffExponent),
                FloatEl("lightInnerConeAngle", s.LightInnerConeAngle),
                FloatEl("lightOuterConeAngle", s.LightOuterConeAngle),
                FloatEl("lightOffset", s.LightOffset),
                new XElement("textureName", s.TextureName),
                UIntEl("sequencerBpm", s.SequencerBpm),
                new XElement("leftHeadLight", UIntEl("sequencer", s.LeftHeadLightSequencer)),
                new XElement("rightHeadLight", UIntEl("sequencer", s.RightHeadLightSequencer)),
                new XElement("leftTailLight", UIntEl("sequencer", s.LeftTailLightSequencer)),
                new XElement("rightTailLight", UIntEl("sequencer", s.RightTailLightSequencer)),
                IntEl("leftHeadLightMultiples", s.LeftHeadLightMultiples),
                IntEl("rightHeadLightMultiples", s.RightHeadLightMultiples),
                IntEl("leftTailLightMultiples", s.LeftTailLightMultiples),
                IntEl("rightTailLightMultiples", s.RightTailLightMultiples),
                BoolEl("useRealLights", s.UseRealLights),
                sirenListEl
            ));
        }

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null),
            new XElement("CVehicleModelInfoVarGlobal",
                new XElement("Kits"),
                new XElement("Lights"),
                sirensEl
            )
        );
        doc.Save(filePath);
    }

    #endregion

    #region vehicles.meta

    public VehicleMetaData LoadVehicleMeta(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var item = doc.Descendants("Item").First();

        return new VehicleMetaData
        {
            ModelName = item.Element("modelName")?.Value ?? "",
            TxdName = item.Element("txdName")?.Value ?? "",
            HandlingId = item.Element("handlingId")?.Value ?? "",
            GameName = item.Element("gameName")?.Value ?? "",
            VehicleMakeName = item.Element("vehicleMakeName")?.Value ?? "",
            Type = GetStr(item, "type"),
            PlateType = GetStr(item, "plateType"),
            DashboardType = GetStr(item, "dashboardType"),
            VehicleClass = GetStr(item, "vehicleClass"),
            WheelType = GetStr(item, "wheelType"),
            Layout = item.Element("layout")?.Value ?? "",
            Flags = GetStr(item, "flags"),
            StrFlags = GetStr(item, "strFlags"),
            AudioNameHash = item.Element("audioNameHash")?.Value ?? "",
            Swankness = GetStr(item, "swankness"),
            MaxPassengers = GetInt(item, "maxPassengers"),
            Mass = GetFloat(item, "mass"),
            PercentSubmergedLevel = GetFloat(item, "percentSubmerged"),
            PrevehicleConvRoofDismount = GetFloat(item, "prevehicleConvRoofDismount"),
            SearchLight = GetFloat(item, "searchLight"),
            DiffuseColor = GetInt(item, "diffuseColor"),
            Rewards = item.Element("rewards")?.Value ?? "",
            CinematicPartCamera = item.Element("cinematicPartCamera")?.Value ?? "",
        };
    }

    public void SaveVehicleMeta(VehicleMetaData data, string filePath)
    {
        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null),
            new XElement("CVehicleModelInfo__InitDataList",
                new XElement("residentTxd"),
                new XElement("residentAnims"),
                new XElement("InitDatas",
                    new XElement("Item",
                        new XElement("modelName", data.ModelName),
                        new XElement("txdName", data.TxdName),
                        new XElement("handlingId", data.HandlingId),
                        new XElement("gameName", data.GameName),
                        new XElement("vehicleMakeName", data.VehicleMakeName),
                        ValEl("type", data.Type),
                        ValEl("plateType", data.PlateType),
                        ValEl("dashboardType", data.DashboardType),
                        ValEl("vehicleClass", data.VehicleClass),
                        ValEl("wheelType", data.WheelType),
                        new XElement("layout", data.Layout),
                        ValEl("flags", data.Flags),
                        ValEl("strFlags", data.StrFlags),
                        new XElement("audioNameHash", data.AudioNameHash),
                        ValEl("swankness", data.Swankness),
                        IntEl("maxPassengers", data.MaxPassengers),
                        FloatEl("mass", data.Mass),
                        FloatEl("percentSubmerged", data.PercentSubmergedLevel),
                        FloatEl("prevehicleConvRoofDismount", data.PrevehicleConvRoofDismount),
                        FloatEl("searchLight", data.SearchLight),
                        IntEl("diffuseColor", data.DiffuseColor),
                        new XElement("rewards", data.Rewards),
                        new XElement("cinematicPartCamera", data.CinematicPartCamera)
                    )
                )
            )
        );
        doc.Save(filePath);
    }

    #endregion

    #region SubHandling Parsers

    private CCarHandlingData ParseCCarHandling(XElement el) => new()
    {
        FBackEndPopUpCarImpulseMult = GetFloat(el, "fBackEndPopUpCarImpulseMult"),
        FBackEndPopUpBuildingImpulseMult = GetFloat(el, "fBackEndPopUpBuildingImpulseMult"),
        FBackEndPopUpMaxDeltaSpeed = GetFloat(el, "fBackEndPopUpMaxDeltaSpeed"),
        FToeFront = GetFloat(el, "fToeFront"),
        FToeRear = GetFloat(el, "fToeRear"),
        FCamberFront = GetFloat(el, "fCamberFront"),
        FCamberRear = GetFloat(el, "fCamberRear"),
        FCastor = GetFloat(el, "fCastor"),
        FEngineResistance = GetFloat(el, "fEngineResistance"),
        FMaxDriveBiasTransfer = GetFloat(el, "fMaxDriveBiasTransfer"),
        FJumpForceScale = GetFloat(el, "fJumpForceScale"),
        NAdvancedFlags = GetInt(el, "nAdvancedFlags"),
    };

    private CBikeHandlingData ParseCBikeHandling(XElement el) => new()
    {
        FLeanFwdCOMMult = GetFloat(el, "fLeanFwdCOMMult"),
        FLeanFwdForceMult = GetFloat(el, "fLeanFwdForceMult"),
        FLeanBakCOMMult = GetFloat(el, "fLeanBakCOMMult"),
        FLeanBakForceMult = GetFloat(el, "fLeanBakForceMult"),
        FMaxBankAngle = GetFloat(el, "fMaxBankAngle"),
        FFullAnimAngle = GetFloat(el, "fFullAnimAngle"),
        FDesLeanReturnFrac = GetFloat(el, "fDesLeanReturnFrac"),
        FStickLeanMult = GetFloat(el, "fStickLeanMult"),
        FBrakingStabilityMult = GetFloat(el, "fBrakingStabilityMult"),
        FInAirSteerMult = GetFloat(el, "fInAirSteerMult"),
        FWheelieBalancePoint = GetFloat(el, "fWheelieBalancePoint"),
        FStoppieBalancePoint = GetFloat(el, "fStoppieBalancePoint"),
        FWheelieSteerMult = GetFloat(el, "fWheelieSteerMult"),
        FRearBalanceMult = GetFloat(el, "fRearBalanceMult"),
        FFrontBalanceMult = GetFloat(el, "fFrontBalanceMult"),
        FBikeGroundSideFrictionMult = GetFloat(el, "fBikeGroundSideFrictionMult"),
        FBikeWheelGroundSideFrictionMult = GetFloat(el, "fBikeWheelGroundSideFrictionMult"),
        FBikeOnStandLeanAngle = GetFloat(el, "fBikeOnStandLeanAngle"),
        FBikeOnStandSteerAngle = GetFloat(el, "fBikeOnStandSteerAngle"),
        FJumpForce = GetFloat(el, "fJumpForce"),
    };

    private CBoatHandlingData ParseCBoatHandling(XElement el) => new()
    {
        FBoxFrontMult = GetFloat(el, "fBoxFrontMult"),
        FBoxRearMult = GetFloat(el, "fBoxRearMult"),
        FBoxSideMult = GetFloat(el, "fBoxSideMult"),
        FSampleTop = GetFloat(el, "fSampleTop"),
        FSampleBottom = GetFloat(el, "fSampleBottom"),
        FAquaplaneForce = GetFloat(el, "fAquaplaneForce"),
        FAquaplanePushWaterMult = GetFloat(el, "fAquaplanePushWaterMult"),
        FAquaplanePushWaterCap = GetFloat(el, "fAquaplanePushWaterCap"),
        FAquaplanePushWaterApply = GetFloat(el, "fAquaplanePushWaterApply"),
        FRudderForce = GetFloat(el, "fRudderForce"),
        FRudderOffsetSubmerge = GetFloat(el, "fRudderOffsetSubmerge"),
        FRudderOffsetForce = GetFloat(el, "fRudderOffsetForce"),
        FRudderOffsetForceZMult = GetFloat(el, "fRudderOffsetForceZMult"),
        FWaveAudioMult = GetFloat(el, "fWaveAudioMult"),
        FDragCoefficient = GetFloat(el, "fDragCoefficient"),
        FKeelSphereSize = GetFloat(el, "fKeelSphereSize"),
        FPropRadius = GetFloat(el, "fPropRadius"),
        FLowLodAngOffset = GetFloat(el, "fLowLodAngOffset"),
        FLowLodDraughtOffset = GetFloat(el, "fLowLodDraughtOffset"),
        FImpellerOffset = GetFloat(el, "fImpellerOffset"),
        FImpellerForceMult = GetFloat(el, "fImpellerForceMult"),
        FDinghySphereBuoyConst = GetFloat(el, "fDinghySphereBuoyConst"),
        FProwRaiseMult = GetFloat(el, "fProwRaiseMult"),
        FDeepSurfaceImpulseMult = GetFloat(el, "fDeepSurfaceImpulseMult"),
        FDeepSurfaceFrictionMult = GetFloat(el, "fDeepSurfaceFrictionMult"),
        FWindMult = GetFloat(el, "fWindMult"),
    };

    private CFlyingHandlingData ParseCFlyingHandling(XElement el) => new()
    {
        FThrust = GetFloat(el, "fThrust"),
        FThrustFallOff = GetFloat(el, "fThrustFallOff"),
        FThrustVectoring = GetFloat(el, "fThrustVectoring"),
        FYawMult = GetFloat(el, "fYawMult"),
        FYawStabilise = GetFloat(el, "fYawStabilise"),
        FSideSlipMult = GetFloat(el, "fSideSlipMult"),
        FRollMult = GetFloat(el, "fRollMult"),
        FRollStabilise = GetFloat(el, "fRollStabilise"),
        FPitchMult = GetFloat(el, "fPitchMult"),
        FPitchStabilise = GetFloat(el, "fPitchStabilise"),
        FFormLiftMult = GetFloat(el, "fFormLiftMult"),
        FAttackLiftMult = GetFloat(el, "fAttackLiftMult"),
        FAttackDiveMult = GetFloat(el, "fAttackDiveMult"),
        FGearDownDragV = GetFloat(el, "fGearDownDragV"),
        FGearDownLiftMult = GetFloat(el, "fGearDownLiftMult"),
        FWindMult = GetFloat(el, "fWindMult"),
        FMoveRes = GetFloat(el, "fMoveRes"),
        VecTurnRes = GetVec3(el, "vecTurnRes"),
        VecSpeedRes = GetVec3(el, "vecSpeedRes"),
        FGearDoorFrontOpen = GetFloat(el, "fGearDoorFrontOpen"),
        FGearDoorRearOpen = GetFloat(el, "fGearDoorRearOpen"),
        FGearDoorRearOpen2 = GetFloat(el, "fGearDoorRearOpen2"),
        FGearDoorRearMOpen = GetFloat(el, "fGearDoorRearMOpen"),
        FTurboThrust = GetFloat(el, "fTurboThrust"),
        FPitchTurboAdjust = GetFloat(el, "fPitchTurboAdjust"),
        FHoverVelMult = GetFloat(el, "fHoverVelMult"),
        HandlingType = GetStr(el, "handlingType"),
    };

    private XElement SerializeSubHandling(IEnumerable<SubHandlingDataBase> items)
    {
        var el = new XElement("SubHandlingData");
        foreach (var item in items)
        {
            el.Add(item switch
            {
                CCarHandlingData c => new XElement("Item", new XAttribute("type", "CCarHandlingData"),
                    FloatEl("fBackEndPopUpCarImpulseMult", c.FBackEndPopUpCarImpulseMult),
                    FloatEl("fBackEndPopUpBuildingImpulseMult", c.FBackEndPopUpBuildingImpulseMult),
                    FloatEl("fBackEndPopUpMaxDeltaSpeed", c.FBackEndPopUpMaxDeltaSpeed),
                    FloatEl("fToeFront", c.FToeFront), FloatEl("fToeRear", c.FToeRear),
                    FloatEl("fCamberFront", c.FCamberFront), FloatEl("fCamberRear", c.FCamberRear),
                    FloatEl("fCastor", c.FCastor), FloatEl("fEngineResistance", c.FEngineResistance),
                    FloatEl("fMaxDriveBiasTransfer", c.FMaxDriveBiasTransfer),
                    FloatEl("fJumpForceScale", c.FJumpForceScale),
                    IntEl("nAdvancedFlags", c.NAdvancedFlags)),
                CBikeHandlingData b => new XElement("Item", new XAttribute("type", "CBikeHandlingData"),
                    FloatEl("fLeanFwdCOMMult", b.FLeanFwdCOMMult), FloatEl("fLeanFwdForceMult", b.FLeanFwdForceMult),
                    FloatEl("fLeanBakCOMMult", b.FLeanBakCOMMult), FloatEl("fLeanBakForceMult", b.FLeanBakForceMult),
                    FloatEl("fMaxBankAngle", b.FMaxBankAngle), FloatEl("fFullAnimAngle", b.FFullAnimAngle),
                    FloatEl("fDesLeanReturnFrac", b.FDesLeanReturnFrac), FloatEl("fStickLeanMult", b.FStickLeanMult),
                    FloatEl("fBrakingStabilityMult", b.FBrakingStabilityMult),
                    FloatEl("fInAirSteerMult", b.FInAirSteerMult),
                    FloatEl("fWheelieBalancePoint", b.FWheelieBalancePoint),
                    FloatEl("fStoppieBalancePoint", b.FStoppieBalancePoint),
                    FloatEl("fWheelieSteerMult", b.FWheelieSteerMult),
                    FloatEl("fRearBalanceMult", b.FRearBalanceMult),
                    FloatEl("fFrontBalanceMult", b.FFrontBalanceMult),
                    FloatEl("fBikeGroundSideFrictionMult", b.FBikeGroundSideFrictionMult),
                    FloatEl("fBikeWheelGroundSideFrictionMult", b.FBikeWheelGroundSideFrictionMult),
                    FloatEl("fBikeOnStandLeanAngle", b.FBikeOnStandLeanAngle),
                    FloatEl("fBikeOnStandSteerAngle", b.FBikeOnStandSteerAngle),
                    FloatEl("fJumpForce", b.FJumpForce)),
                CBoatHandlingData bt => new XElement("Item", new XAttribute("type", "CBoatHandlingData"),
                    FloatEl("fBoxFrontMult", bt.FBoxFrontMult), FloatEl("fBoxRearMult", bt.FBoxRearMult),
                    FloatEl("fBoxSideMult", bt.FBoxSideMult),
                    FloatEl("fSampleTop", bt.FSampleTop), FloatEl("fSampleBottom", bt.FSampleBottom),
                    FloatEl("fAquaplaneForce", bt.FAquaplaneForce),
                    FloatEl("fAquaplanePushWaterMult", bt.FAquaplanePushWaterMult),
                    FloatEl("fAquaplanePushWaterCap", bt.FAquaplanePushWaterCap),
                    FloatEl("fAquaplanePushWaterApply", bt.FAquaplanePushWaterApply),
                    FloatEl("fRudderForce", bt.FRudderForce),
                    FloatEl("fRudderOffsetSubmerge", bt.FRudderOffsetSubmerge),
                    FloatEl("fRudderOffsetForce", bt.FRudderOffsetForce),
                    FloatEl("fRudderOffsetForceZMult", bt.FRudderOffsetForceZMult),
                    FloatEl("fWaveAudioMult", bt.FWaveAudioMult),
                    FloatEl("fDragCoefficient", bt.FDragCoefficient),
                    FloatEl("fKeelSphereSize", bt.FKeelSphereSize),
                    FloatEl("fPropRadius", bt.FPropRadius),
                    FloatEl("fLowLodAngOffset", bt.FLowLodAngOffset),
                    FloatEl("fLowLodDraughtOffset", bt.FLowLodDraughtOffset),
                    FloatEl("fImpellerOffset", bt.FImpellerOffset),
                    FloatEl("fImpellerForceMult", bt.FImpellerForceMult),
                    FloatEl("fDinghySphereBuoyConst", bt.FDinghySphereBuoyConst),
                    FloatEl("fProwRaiseMult", bt.FProwRaiseMult),
                    FloatEl("fDeepSurfaceImpulseMult", bt.FDeepSurfaceImpulseMult),
                    FloatEl("fDeepSurfaceFrictionMult", bt.FDeepSurfaceFrictionMult),
                    FloatEl("fWindMult", bt.FWindMult)),
                CFlyingHandlingData f => new XElement("Item", new XAttribute("type", "CFlyingHandlingData"),
                    FloatEl("fThrust", f.FThrust), FloatEl("fThrustFallOff", f.FThrustFallOff),
                    FloatEl("fThrustVectoring", f.FThrustVectoring),
                    FloatEl("fYawMult", f.FYawMult), FloatEl("fYawStabilise", f.FYawStabilise),
                    FloatEl("fSideSlipMult", f.FSideSlipMult),
                    FloatEl("fRollMult", f.FRollMult), FloatEl("fRollStabilise", f.FRollStabilise),
                    FloatEl("fPitchMult", f.FPitchMult), FloatEl("fPitchStabilise", f.FPitchStabilise),
                    FloatEl("fFormLiftMult", f.FFormLiftMult),
                    FloatEl("fAttackLiftMult", f.FAttackLiftMult), FloatEl("fAttackDiveMult", f.FAttackDiveMult),
                    FloatEl("fGearDownDragV", f.FGearDownDragV), FloatEl("fGearDownLiftMult", f.FGearDownLiftMult),
                    FloatEl("fWindMult", f.FWindMult), FloatEl("fMoveRes", f.FMoveRes),
                    Vec3El("vecTurnRes", f.VecTurnRes), Vec3El("vecSpeedRes", f.VecSpeedRes),
                    FloatEl("fGearDoorFrontOpen", f.FGearDoorFrontOpen),
                    FloatEl("fGearDoorRearOpen", f.FGearDoorRearOpen),
                    FloatEl("fGearDoorRearOpen2", f.FGearDoorRearOpen2),
                    FloatEl("fGearDoorRearMOpen", f.FGearDoorRearMOpen),
                    FloatEl("fTurboThrust", f.FTurboThrust),
                    FloatEl("fPitchTurboAdjust", f.FPitchTurboAdjust),
                    FloatEl("fHoverVelMult", f.FHoverVelMult),
                    ValEl("handlingType", f.HandlingType)),
                _ => new XElement("Item", new XAttribute("type", item.TypeName))
            });
        }
        return el;
    }

    #endregion

    #region XML Helpers

    private static float GetFloat(XElement? parent, string name)
    {
        var val = parent?.Element(name)?.Attribute("value")?.Value;
        return float.TryParse(val, NumberStyles.Float, Inv, out var f) ? f : 0f;
    }

    private static int GetInt(XElement? parent, string name)
    {
        var val = parent?.Element(name)?.Attribute("value")?.Value;
        return int.TryParse(val, NumberStyles.Integer, Inv, out var i) ? i : 0;
    }

    private static uint GetUInt(XElement? parent, string name)
    {
        var val = parent?.Element(name)?.Attribute("value")?.Value;
        return uint.TryParse(val, NumberStyles.Integer, Inv, out var u) ? u : 0;
    }

    private static bool GetBool(XElement? parent, string name)
    {
        var val = parent?.Element(name)?.Attribute("value")?.Value;
        return val == "true";
    }

    private static string GetStr(XElement? parent, string name)
    {
        return parent?.Element(name)?.Attribute("value")?.Value
            ?? parent?.Element(name)?.Value
            ?? "";
    }

    private static Vector3 GetVec3(XElement? parent, string name)
    {
        var el = parent?.Element(name);
        if (el == null) return Vector3.Zero;
        return new Vector3(
            float.TryParse(el.Attribute("x")?.Value, NumberStyles.Float, Inv, out var x) ? x : 0f,
            float.TryParse(el.Attribute("y")?.Value, NumberStyles.Float, Inv, out var y) ? y : 0f,
            float.TryParse(el.Attribute("z")?.Value, NumberStyles.Float, Inv, out var z) ? z : 0f
        );
    }

    private static XElement FloatEl(string name, float val)
        => new(name, new XAttribute("value", val.ToString("F6", Inv)));

    private static XElement IntEl(string name, int val)
        => new(name, new XAttribute("value", val));

    private static XElement UIntEl(string name, uint val)
        => new(name, new XAttribute("value", val));

    private static XElement BoolEl(string name, bool val)
        => new(name, new XAttribute("value", val ? "true" : "false"));

    private static XElement ValEl(string name, string val)
        => new(name, new XAttribute("value", val));

    private static XElement Vec3El(string name, Vector3 v)
        => new(name,
            new XAttribute("x", v.X.ToString("F6", Inv)),
            new XAttribute("y", v.Y.ToString("F6", Inv)),
            new XAttribute("z", v.Z.ToString("F6", Inv)));

    #endregion
}
