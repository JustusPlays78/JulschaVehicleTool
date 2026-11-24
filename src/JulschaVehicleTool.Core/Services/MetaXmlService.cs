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
        var item = HandlingItems(doc).FirstOrDefault();
        if (item == null)
            return new HandlingData(); // empty template -> return defaults
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
            SourceXml = item.ToString(),
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
                    BuildHandlingItem(data)
                )
            )
        );
        doc.Save(filePath);
    }

    /// <summary>
    /// Builds the &lt;Item&gt; for handling.meta. If the data was imported, the original XML is
    /// kept and only fields changed in the tool are patched into it.
    /// </summary>
    public XElement BuildHandlingItem(HandlingData d)
    {
        var generated = GenerateHandlingItem(d);
        if (string.IsNullOrWhiteSpace(d.SourceXml))
            return generated;

        var source = XElement.Parse(d.SourceXml);
        var baseline = GenerateHandlingItem(ParseHandlingItem(source));
        MergeChanged(source, generated, baseline, skip: ["SubHandlingData"]);
        MergeSubHandling(source, generated.Element("SubHandlingData"), baseline.Element("SubHandlingData"));
        return source;
    }

    private static readonly HashSet<string> KnownSubHandlingTypes =
        ["CCarHandlingData", "CBikeHandlingData", "CBoatHandlingData", "CFlyingHandlingData"];

    /// <summary>
    /// Patches SubHandlingData item by item. Items of types the tool does not model
    /// (e.g. type="NULL", CSpecialFlightHandlingData, ...) are preserved untouched.
    /// </summary>
    private static void MergeSubHandling(XElement source, XElement? generated, XElement? baseline)
    {
        if (generated == null || baseline == null || XNode.DeepEquals(generated, baseline))
            return;

        var target = source.Element("SubHandlingData");
        if (target == null)
        {
            source.Add(new XElement(generated));
            return;
        }

        bool IsKnown(XElement e) => KnownSubHandlingTypes.Contains(e.Attribute("type")?.Value ?? "");
        var targetKnown = target.Elements("Item").Where(IsKnown).ToList();
        var genItems = generated.Elements("Item").ToList();
        var baseItems = baseline.Elements("Item").ToList();

        bool sameShape = targetKnown.Count == genItems.Count && baseItems.Count == genItems.Count
            && genItems.Select(g => g.Attribute("type")?.Value)
                       .SequenceEqual(baseItems.Select(b => b.Attribute("type")?.Value));

        if (sameShape)
        {
            for (int i = 0; i < genItems.Count; i++)
                MergeChanged(targetKnown[i], genItems[i], baseItems[i]);
            return;
        }

        // Items were added/removed in the tool: rebuild the known items, keep unknown ones.
        foreach (var e in targetKnown) e.Remove();
        foreach (var g in genItems) target.Add(new XElement(g));
    }

    private XElement GenerateHandlingItem(HandlingData d)
    {
        var item = new XElement("Item", new XAttribute("type", "CHandlingData"),
            TextEl("handlingName", d.HandlingName),
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
            TextEl("strModelFlags", d.StrModelFlags),
            TextEl("strHandlingFlags", d.StrHandlingFlags),
            TextEl("strDamageFlags", d.StrDamageFlags),
            TextEl("AIHandling", d.AiHandling),
            SerializeSubHandling(d.SubHandlingItems)
        );
        return item;
    }

    #endregion

    #region carvariations.meta

    public CarVariationData LoadCarVariations(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var variationData = doc.Descendants("variationData").FirstOrDefault()
            ?? throw new InvalidOperationException($"No <variationData> element found in {Path.GetFileName(filePath)}. Is this a valid carvariations.meta?");
        var item = variationData.Element("Item")
            ?? throw new InvalidOperationException($"No <Item> element found in {Path.GetFileName(filePath)}.");

        return ParseCarVariationItem(item);
    }

    /// <summary>
    /// Loads all car variation entries from a carvariations.meta file, keyed by modelName.
    /// </summary>
    public Dictionary<string, CarVariationData> LoadAllCarVariations(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var result = new Dictionary<string, CarVariationData>(StringComparer.OrdinalIgnoreCase);

        var variationData = doc.Descendants("variationData").FirstOrDefault();
        if (variationData == null) return result;

        foreach (var item in variationData.Elements("Item"))
        {
            var data = ParseCarVariationItem(item);
            if (!string.IsNullOrEmpty(data.ModelName))
                result[data.ModelName] = data;
        }

        return result;
    }

    public CarVariationData ParseCarVariationItem(XElement item)
    {
        var data = new CarVariationData
        {
            ModelName = item.Element("modelName")?.Value.Trim() ?? "",
            LightSettings = GetInt(item, "lightSettings"),
            SirenSettings = GetInt(item, "sirenSettings"),
            SourceXml = item.ToString(),
        };

        var colors = item.Element("colors");
        if (colors != null)
        {
            foreach (var colorItem in colors.Elements("Item"))
            {
                var combo = new ColorCombination();
                var indices = (colorItem.Element("indices")?.Value ?? "")
                    .Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s, NumberStyles.Integer, Inv, out var v) ? v : 0)
                    .ToArray();
                if (indices.Length >= 1) combo.PrimaryColor = indices[0];
                if (indices.Length >= 2) combo.SecondaryColor = indices[1];
                if (indices.Length >= 3) combo.PearlescentColor = indices[2];
                if (indices.Length >= 4) combo.RimColor = indices[3];
                if (indices.Length >= 5) combo.InteriorTrimColor = indices[4];
                if (indices.Length >= 6) combo.DashboardColor = indices[5];

                var liveries = colorItem.Element("liveries");
                if (liveries != null)
                    foreach (var liv in liveries.Elements("Item"))
                        combo.Liveries.Add(liv.Attribute("value")?.Value == "true");

                data.Colors.Add(combo);
            }
        }

        var kits = item.Element("kits");
        if (kits != null)
            foreach (var kit in kits.Elements("Item"))
                data.Kits.Add(kit.Value.Trim());

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

    /// <summary>
    /// Builds the carvariations &lt;Item&gt;, patching changes into the imported XML if present.
    /// </summary>
    public XElement BuildCarVariationItem(CarVariationData data)
    {
        var generated = GenerateCarVariationItem(data);
        if (string.IsNullOrWhiteSpace(data.SourceXml))
            return generated;

        var source = XElement.Parse(data.SourceXml);
        var baseline = GenerateCarVariationItem(ParseCarVariationItem(source));
        MergeChanged(source, generated, baseline);
        return source;
    }

    private XElement GenerateCarVariationItem(CarVariationData data)
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

        var probabilitiesEl = new XElement("Probabilities");
        foreach (var plate in data.PlateProbabilities)
        {
            probabilitiesEl.Add(new XElement("Item",
                new XElement("Name", plate.Name),
                new XElement("Value", new XAttribute("value", plate.Value))
            ));
        }

        return new XElement("Item",
            new XElement("modelName", data.ModelName),
            colorsEl,
            kitsEl,
            new XElement("windowsWithExposedEdges"),
            new XElement("plateProbabilities", probabilitiesEl),
            new XElement("lightSettings", new XAttribute("value", data.LightSettings)),
            new XElement("sirenSettings", new XAttribute("value", data.SirenSettings))
        );
    }

    public void SaveCarVariations(CarVariationData data, string filePath)
    {
        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null),
            new XElement("CVehicleModelInfoVariation",
                new XElement("variationData", BuildCarVariationItem(data))
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

        var sirens = doc.Root?.Element("Sirens") ?? doc.Descendants("Sirens").FirstOrDefault();
        if (sirens != null)
        {
            foreach (var sirenItem in sirens.Elements("Item"))
            {
                data.SirenSettings.Add(ParseSirenSetting(sirenItem));
            }
        }

        var kits = doc.Root?.Element("Kits");
        if (kits != null)
            foreach (var item in kits.Elements("Item"))
                data.Kits.Add(new CarColsEntry { Id = GetInt(item, "id"), Name = GetText(item, "kitName"), Xml = item.ToString() });

        var lights = doc.Root?.Element("Lights");
        if (lights != null)
            foreach (var item in lights.Elements("Item"))
                data.Lights.Add(new CarColsEntry { Id = GetInt(item, "id"), Name = GetText(item, "name"), Xml = item.ToString() });

        return data;
    }

    public SirenSetting ParseSirenSetting(XElement sirenItem)
    {
        var setting = new SirenSetting
        {
            Id = GetInt(sirenItem, "id"),
            Name = sirenItem.Element("name")?.Value ?? "",
            SourceXml = sirenItem.ToString(),
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
        return setting;
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

    /// <summary>Builds a siren &lt;Item&gt;, patching changes into the imported XML if present.</summary>
    public XElement BuildSirenSetting(SirenSetting s)
    {
        var generated = GenerateSirenSetting(s);
        if (string.IsNullOrWhiteSpace(s.SourceXml))
            return generated;

        var source = XElement.Parse(s.SourceXml);
        var baseline = GenerateSirenSetting(ParseSirenSetting(source));
        MergeChanged(source, generated, baseline);
        return source;
    }

    private XElement GenerateSirenSetting(SirenSetting s)
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

        return new XElement("Item",
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
        );
    }

    public void SaveCarCols(CarColsData data, string filePath)
    {
        var sirensEl = new XElement("Sirens");
        foreach (var s in data.SirenSettings)
            sirensEl.Add(BuildSirenSetting(s));

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null),
            new XElement("CVehicleModelInfoVarGlobal",
                new XElement("Kits", data.Kits.Select(k => XElement.Parse(k.Xml))),
                new XElement("Lights", data.Lights.Select(l => XElement.Parse(l.Xml))),
                sirensEl
            )
        );
        doc.Save(filePath);
    }

    #endregion

    #region vehicles.meta

    /// <summary>
    /// Fields of the tool's VehicleMetaData that are not part of the vanilla vehicles.meta schema.
    /// They are only written back if the imported XML already contained them.
    /// </summary>
    private static readonly HashSet<string> NonVanillaVehicleFields =
        ["maxPassengers", "mass", "percentSubmerged", "prevehicleConvRoofDismount", "searchLight", "diffuseColor", "strFlags"];

    public VehicleMetaData LoadVehicleMeta(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var item = doc.Descendants("InitDatas").FirstOrDefault()?.Elements("Item").FirstOrDefault()
            ?? throw new InvalidOperationException($"No <InitDatas><Item> element found in {Path.GetFileName(filePath)}. Is this a valid vehicles.meta?");

        var data = ParseVehicleMetaItem(item);
        AttachTxdRelationships(data, ParseTxdRelationships(doc));
        return data;
    }

    /// <summary>
    /// Loads all vehicle meta entries from a vehicles.meta file, keyed by modelName.
    /// txdRelationships relevant to each vehicle are attached to it.
    /// </summary>
    public Dictionary<string, VehicleMetaData> LoadAllVehicleMetas(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var result = new Dictionary<string, VehicleMetaData>(StringComparer.OrdinalIgnoreCase);

        var initDatas = doc.Descendants("InitDatas").FirstOrDefault();
        if (initDatas == null) return result;

        var relationships = ParseTxdRelationships(doc);

        foreach (var item in initDatas.Elements("Item"))
        {
            var data = ParseVehicleMetaItem(item);
            if (string.IsNullOrEmpty(data.ModelName)) continue;

            AttachTxdRelationships(data, relationships);
            result[data.ModelName] = data;
        }

        return result;
    }

    public VehicleMetaData ParseVehicleMetaItem(XElement item)
    {
        return new VehicleMetaData
        {
            ModelName = GetText(item, "modelName"),
            TxdName = GetText(item, "txdName"),
            HandlingId = GetText(item, "handlingId"),
            GameName = GetText(item, "gameName"),
            VehicleMakeName = GetText(item, "vehicleMakeName"),
            Type = GetStr(item, "type"),
            PlateType = GetStr(item, "plateType"),
            DashboardType = GetStr(item, "dashboardType"),
            VehicleClass = GetStr(item, "vehicleClass"),
            WheelType = GetStr(item, "wheelType"),
            Layout = GetText(item, "layout"),
            Flags = GetStr(item, "flags").Trim(),
            StrFlags = GetStr(item, "strFlags"),
            AudioNameHash = GetText(item, "audioNameHash"),
            Swankness = GetStr(item, "swankness"),
            MaxPassengers = GetInt(item, "maxPassengers"),
            Mass = GetFloat(item, "mass"),
            PercentSubmergedLevel = GetFloat(item, "percentSubmerged"),
            PrevehicleConvRoofDismount = GetFloat(item, "prevehicleConvRoofDismount"),
            SearchLight = GetFloat(item, "searchLight"),
            DiffuseColor = GetInt(item, "diffuseColor"),
            Rewards = GetItemList(item, "rewards"),
            CinematicPartCamera = GetItemList(item, "cinematicPartCamera"),
            SourceXml = item.ToString(),
        };
    }

    /// <summary>
    /// Builds the vehicles.meta &lt;Item&gt;. Imported vehicles keep their original XML and only
    /// changed fields are patched; new vehicles start from a complete vanilla-style template.
    /// </summary>
    public XElement BuildVehicleMetaItem(VehicleMetaData data)
    {
        var source = string.IsNullOrWhiteSpace(data.SourceXml)
            ? VehicleMetaTemplate.Create()
            : XElement.Parse(data.SourceXml);

        var generated = GenerateVehicleMetaItem(data);
        var baseline = GenerateVehicleMetaItem(ParseVehicleMetaItem(source));

        var absent = NonVanillaVehicleFields.Where(f => source.Element(f) == null).ToArray();
        MergeChanged(source, generated, baseline, skip: absent);
        return source;
    }

    private XElement GenerateVehicleMetaItem(VehicleMetaData data)
    {
        return new XElement("Item",
            TextEl("modelName", data.ModelName),
            TextEl("txdName", data.TxdName),
            TextEl("handlingId", data.HandlingId),
            TextEl("gameName", data.GameName),
            TextEl("vehicleMakeName", data.VehicleMakeName),
            TextEl("type", data.Type),
            TextEl("plateType", data.PlateType),
            TextEl("dashboardType", data.DashboardType),
            TextEl("vehicleClass", data.VehicleClass),
            TextEl("wheelType", data.WheelType),
            TextEl("layout", data.Layout),
            TextEl("flags", data.Flags),
            TextEl("strFlags", data.StrFlags),
            TextEl("audioNameHash", data.AudioNameHash),
            TextEl("swankness", data.Swankness),
            IntEl("maxPassengers", data.MaxPassengers),
            FloatEl("mass", data.Mass),
            FloatEl("percentSubmerged", data.PercentSubmergedLevel),
            FloatEl("prevehicleConvRoofDismount", data.PrevehicleConvRoofDismount),
            FloatEl("searchLight", data.SearchLight),
            IntEl("diffuseColor", data.DiffuseColor),
            ItemListEl("rewards", data.Rewards),
            ItemListEl("cinematicPartCamera", data.CinematicPartCamera)
        );
    }

    public void SaveVehicleMeta(VehicleMetaData data, string filePath)
    {
        var item = BuildVehicleMetaItem(data);

        var relationshipsEl = new XElement("txdRelationships");
        foreach (var rel in GetExportTxdRelationships(data))
            relationshipsEl.Add(new XElement("Item",
                new XElement("parent", rel.Parent),
                new XElement("child", rel.Child)));

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null),
            new XElement("CVehicleModelInfo__InitDataList",
                new XElement("residentTxd", "vehshare"),
                new XElement("residentAnims"),
                new XElement("InitDatas", item),
                relationshipsEl
            )
        );
        doc.Save(filePath);
    }

    /// <summary>
    /// Returns the txdRelationships to export. If the txdName was renamed in the tool,
    /// relationships pointing at the old txd name are redirected to the new one.
    /// </summary>
    private IEnumerable<TxdRelationship> GetExportTxdRelationships(VehicleMetaData data)
    {
        string? originalTxd = null;
        if (!string.IsNullOrWhiteSpace(data.SourceXml))
            originalTxd = GetText(XElement.Parse(data.SourceXml), "txdName");

        var seen = new HashSet<(string, string)>();
        foreach (var rel in data.TxdRelationships)
        {
            var parent = rel.Parent;
            var child = rel.Child;
            if (!string.IsNullOrEmpty(originalTxd) && !string.IsNullOrEmpty(data.TxdName)
                && !originalTxd.Equals(data.TxdName, StringComparison.OrdinalIgnoreCase))
            {
                if (child.Equals(originalTxd, StringComparison.OrdinalIgnoreCase)) child = data.TxdName;
                if (parent.Equals(originalTxd, StringComparison.OrdinalIgnoreCase)) parent = data.TxdName;
            }

            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(child)) continue;
            if (seen.Add((parent.ToLowerInvariant(), child.ToLowerInvariant())))
                yield return new TxdRelationship { Parent = parent, Child = child };
        }
    }

    private static List<TxdRelationship> ParseTxdRelationships(XDocument doc)
    {
        var list = new List<TxdRelationship>();
        var rels = doc.Root?.Element("txdRelationships");
        if (rels == null) return list;

        foreach (var item in rels.Elements("Item"))
        {
            var parent = GetText(item, "parent");
            var child = GetText(item, "child");
            if (parent.Length > 0 && child.Length > 0)
                list.Add(new TxdRelationship { Parent = parent, Child = child });
        }
        return list;
    }

    /// <summary>
    /// Attaches the relationship chain starting at the vehicle's txdName
    /// (e.g. police1 → police_shared → vehshare) to the vehicle.
    /// </summary>
    private static void AttachTxdRelationships(VehicleMetaData data, List<TxdRelationship> all)
    {
        var current = data.TxdName;
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (!string.IsNullOrEmpty(current) && visited.Add(current))
        {
            var rel = all.FirstOrDefault(r => r.Child.Equals(current, StringComparison.OrdinalIgnoreCase));
            if (rel == null) break;
            data.TxdRelationships.Add(new TxdRelationship { Parent = rel.Parent, Child = rel.Child });
            current = rel.Parent;
        }
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
                    TextEl("handlingType", f.HandlingType)),
                _ => new XElement("Item", new XAttribute("type", item.TypeName))
            });
        }
        return el;
    }

    #endregion

    #region Multi-Vehicle Loading (for import matching)

    /// <summary>
    /// Loads all handling entries from a handling.meta file, keyed by handlingName.
    /// </summary>
    public Dictionary<string, HandlingData> LoadAllHandlings(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var result = new Dictionary<string, HandlingData>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in HandlingItems(doc))
        {
            var data = ParseHandlingItem(item);
            if (!string.IsNullOrEmpty(data.HandlingName))
                result[data.HandlingName] = data;
        }

        return result;
    }

    #endregion

    #region XML Patching

    /// <summary>
    /// Copies every child of <paramref name="generated"/> that differs from the same-named child of
    /// <paramref name="baseline"/> into <paramref name="target"/> (replacing it in place, or appending it).
    /// <paramref name="baseline"/> is what the tool would generate from the unmodified source, so a
    /// difference means the user changed that field. Unchanged fields keep their original XML.
    /// </summary>
    internal static void MergeChanged(XElement target, XElement generated, XElement baseline,
        IEnumerable<string>? skip = null)
    {
        var skipSet = skip != null ? new HashSet<string>(skip) : null;

        foreach (var g in generated.Elements())
        {
            var name = g.Name.LocalName;
            if (skipSet?.Contains(name) == true) continue;

            var b = baseline.Element(g.Name);
            if (b != null && XNode.DeepEquals(g, b)) continue;

            var t = target.Element(g.Name);
            if (t != null)
                t.ReplaceWith(new XElement(g));
            else
                target.Add(new XElement(g));
        }
    }

    private static IEnumerable<XElement> HandlingItems(XDocument doc)
    {
        var handlingData = doc.Root?.Element("HandlingData");
        var items = handlingData?.Elements("Item") ?? doc.Descendants("Item");
        return items.Where(i => i.Attribute("type")?.Value is null or "CHandlingData"
                                && i.Element("handlingName") != null);
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

    /// <summary>Reads the trimmed text content of a child element (for string fields stored as text).</summary>
    private static string GetText(XElement? parent, string name)
        => parent?.Element(name)?.Value.Trim() ?? "";

    /// <summary>Reads a list of &lt;Item&gt; children as a space-separated string (falls back to text content).</summary>
    private static string GetItemList(XElement? parent, string name)
    {
        var el = parent?.Element(name);
        if (el == null) return "";
        var items = el.Elements("Item").Select(i => i.Value.Trim()).Where(v => v.Length > 0).ToList();
        return items.Count > 0 ? string.Join(" ", items) : el.Value.Trim();
    }

    /// <summary>Writes a string field as text content: &lt;name&gt;value&lt;/name&gt;.</summary>
    private static XElement TextEl(string name, string? val)
        => string.IsNullOrEmpty(val) ? new XElement(name) : new XElement(name, val);

    /// <summary>Writes a space-separated string as a list of &lt;Item&gt; children.</summary>
    private static XElement ItemListEl(string name, string? val)
        => new(name, (val ?? "").Split([' ', '\t', '\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries)
                                .Select(v => new XElement("Item", v)));

    private static XElement FloatEl(string name, float val)
        => new(name, new XAttribute("value", val.ToString("F6", Inv)));

    private static XElement IntEl(string name, int val)
        => new(name, new XAttribute("value", val));

    private static XElement UIntEl(string name, uint val)
        => new(name, new XAttribute("value", val));

    private static XElement BoolEl(string name, bool val)
        => new(name, new XAttribute("value", val ? "true" : "false"));

    private static XElement Vec3El(string name, Vector3 v)
        => new(name,
            new XAttribute("x", v.X.ToString("F6", Inv)),
            new XAttribute("y", v.Y.ToString("F6", Inv)),
            new XAttribute("z", v.Z.ToString("F6", Inv)));

    #endregion
}
