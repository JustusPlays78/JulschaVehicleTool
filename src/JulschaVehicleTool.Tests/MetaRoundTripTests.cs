using System.Text.Json;
using System.Xml.Linq;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Serialization;
using JulschaVehicleTool.Core.Services;

namespace JulschaVehicleTool.Tests;

/// <summary>
/// Import → export must not lose or reformat data the tool does not model,
/// and edited fields must be written in the format GTA expects.
/// </summary>
public class MetaRoundTripTests : IDisposable
{
    private readonly MetaXmlService _svc = new();
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "jvt_tests_" + Guid.NewGuid().ToString("N"));

    public MetaRoundTripTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    private static string Data(string file) => Path.Combine(AppContext.BaseDirectory, "TestData", file);
    private string Temp(string file) => Path.Combine(_tempDir, file);

    private static XElement HandlingItem(string path) =>
        XDocument.Load(path).Root!.Element("HandlingData")!.Elements("Item").First();

    private static XElement VehicleItem(string path, string modelName) =>
        XDocument.Load(path).Descendants("InitDatas").Single().Elements("Item")
            .Single(i => i.Element("modelName")!.Value == modelName);

    private static void AssertSameXml(XElement expected, XElement actual) =>
        Assert.True(XNode.DeepEquals(expected, actual),
            $"XML differs.\nExpected:\n{expected}\n\nActual:\n{actual}");

    // ---------------- handling.meta ----------------

    [Fact]
    public void Handling_UnchangedRoundTrip_IsIdentical()
    {
        var data = _svc.LoadAllHandlings(Data("handling.meta"))["JVPOLICE"];
        _svc.SaveHandling(data, Temp("handling.meta"));

        AssertSameXml(HandlingItem(Data("handling.meta")), HandlingItem(Temp("handling.meta")));
    }

    [Fact]
    public void Handling_EditedField_IsPatched_RestIsPreserved()
    {
        var data = _svc.LoadAllHandlings(Data("handling.meta"))["JVPOLICE"];
        data.FInitialDriveForce = 0.5f;
        data.StrHandlingFlags = "20002";
        _svc.SaveHandling(data, Temp("handling.meta"));

        var item = HandlingItem(Temp("handling.meta"));
        Assert.Equal("0.500000", item.Element("fInitialDriveForce")!.Attribute("value")!.Value);
        Assert.Equal("20002", item.Element("strHandlingFlags")!.Value);

        // Sub handling: unknown field and NULL placeholders survive
        var sub = item.Element("SubHandlingData")!.Elements("Item").ToList();
        Assert.Equal(3, sub.Count);
        Assert.Equal("8000", sub[0].Element("strAdvancedFlags")!.Value);
        Assert.Equal("NULL", sub[1].Attribute("type")!.Value);
    }

    [Fact]
    public void Handling_NewVehicle_WritesStringsAsText()
    {
        var data = VehicleDefaults.CreateDefaultHandling("mycar");
        _svc.SaveHandling(data, Temp("handling.meta"));

        var item = HandlingItem(Temp("handling.meta"));
        foreach (var name in new[] { "handlingName", "strModelFlags", "strHandlingFlags", "strDamageFlags", "AIHandling" })
        {
            var el = item.Element(name)!;
            Assert.Null(el.Attribute("value"));
            Assert.False(string.IsNullOrEmpty(el.Value), name);
        }
        Assert.Equal("MYCAR", item.Element("handlingName")!.Value);
    }

    // ---------------- vehicles.meta ----------------

    [Fact]
    public void VehicleMeta_UnchangedRoundTrip_IsIdentical()
    {
        foreach (var model in new[] { "jvpolice", "jvambulance" })
        {
            var data = _svc.LoadAllVehicleMetas(Data("vehicles.meta"))[model];
            _svc.SaveVehicleMeta(data, Temp($"{model}.meta"));

            AssertSameXml(VehicleItem(Data("vehicles.meta"), model), VehicleItem(Temp($"{model}.meta"), model));
        }
    }

    [Fact]
    public void VehicleMeta_EditedFields_ArePatchedAsText_UnknownFieldsPreserved()
    {
        var data = _svc.LoadAllVehicleMetas(Data("vehicles.meta"))["jvpolice"];
        data.GameName = "JVPOL2";
        data.Flags = "FLAG_LAW_ENFORCEMENT";
        data.VehicleClass = "VC_SEDAN";
        _svc.SaveVehicleMeta(data, Temp("vehicles.meta"));

        var item = VehicleItem(Temp("vehicles.meta"), "jvpolice");
        Assert.Equal("JVPOL2", item.Element("gameName")!.Value);
        Assert.Equal("FLAG_LAW_ENFORCEMENT", item.Element("flags")!.Value);
        Assert.Equal("VC_SEDAN", item.Element("vehicleClass")!.Value);
        Assert.Null(item.Element("vehicleClass")!.Attribute("value"));

        // Not modelled by the tool → must survive untouched
        var original = VehicleItem(Data("vehicles.meta"), "jvpolice");
        foreach (var name in new[] { "lodDistances", "wheelScale", "drivers", "cinematicPartCamera", "rewards", "firstPersonDrivebyData", "requiredExtras" })
            AssertSameXml(original.Element(name)!, item.Element(name)!);

        // Non-vanilla tool fields must not be injected
        Assert.Null(item.Element("mass"));
        Assert.Null(item.Element("maxPassengers"));
    }

    [Fact]
    public void VehicleMeta_ItemLists_AreParsedAndWrittenAsItems()
    {
        var data = _svc.LoadAllVehicleMetas(Data("vehicles.meta"))["jvpolice"];
        Assert.Equal("REWARD_WEAPON_PUMPSHOTGUN REWARD_STAT_WEAPON", data.Rewards);

        data.Rewards = "REWARD_WEAPON_PUMPSHOTGUN";
        _svc.SaveVehicleMeta(data, Temp("vehicles.meta"));

        var rewards = VehicleItem(Temp("vehicles.meta"), "jvpolice").Element("rewards")!;
        Assert.Equal(["REWARD_WEAPON_PUMPSHOTGUN"], rewards.Elements("Item").Select(i => i.Value));
    }

    [Fact]
    public void VehicleMeta_NewVehicle_UsesCompleteTemplate()
    {
        var data = VehicleDefaults.CreateDefaultVehicleMeta("mycar");
        _svc.SaveVehicleMeta(data, Temp("vehicles.meta"));

        var doc = XDocument.Load(Temp("vehicles.meta"));
        var item = VehicleItem(Temp("vehicles.meta"), "mycar");
        Assert.Equal("VEHICLE_TYPE_CAR", item.Element("type")!.Value);
        Assert.Equal("SWANKNESS_1", item.Element("swankness")!.Value);
        Assert.Equal("MYCAR", item.Element("handlingId")!.Value);
        Assert.NotNull(item.Element("lodDistances"));
        Assert.NotNull(item.Element("wheelScale"));
        Assert.NotNull(item.Element("cameraName"));
        Assert.Null(item.Element("mass"));

        var rel = doc.Root!.Element("txdRelationships")!.Elements("Item").Single();
        Assert.Equal("vehshare", rel.Element("parent")!.Value);
        Assert.Equal("mycar", rel.Element("child")!.Value);
    }

    [Fact]
    public void VehicleMeta_TxdRelationshipChain_IsAttachedAndExported()
    {
        var data = _svc.LoadAllVehicleMetas(Data("vehicles.meta"))["jvpolice"];
        Assert.Equal(2, data.TxdRelationships.Count);

        _svc.SaveVehicleMeta(data, Temp("vehicles.meta"));
        var rels = XDocument.Load(Temp("vehicles.meta")).Root!.Element("txdRelationships")!.Elements("Item")
            .Select(i => (i.Element("parent")!.Value, i.Element("child")!.Value)).ToList();

        Assert.Contains(("jv_emergency_shared", "jvpolice"), rels);
        Assert.Contains(("vehshare", "jv_emergency_shared"), rels);
        Assert.DoesNotContain(rels, r => r.Item2 == "jvambulance");
    }

    [Fact]
    public void VehicleMeta_RenamedTxd_RedirectsRelationship()
    {
        var data = _svc.LoadAllVehicleMetas(Data("vehicles.meta"))["jvpolice"];
        data.TxdName = "jvpolice2";
        _svc.SaveVehicleMeta(data, Temp("vehicles.meta"));

        var rels = XDocument.Load(Temp("vehicles.meta")).Root!.Element("txdRelationships")!.Elements("Item")
            .Select(i => (i.Element("parent")!.Value, i.Element("child")!.Value)).ToList();
        Assert.Contains(("jv_emergency_shared", "jvpolice2"), rels);
    }

    // ---------------- carvariations.meta ----------------

    [Fact]
    public void CarVariations_UnchangedRoundTrip_IsIdentical()
    {
        var data = _svc.LoadCarVariations(Data("carvariations.meta"));
        _svc.SaveCarVariations(data, Temp("carvariations.meta"));

        AssertSameXml(
            XDocument.Load(Data("carvariations.meta")).Descendants("variationData").Single().Element("Item")!,
            XDocument.Load(Temp("carvariations.meta")).Descendants("variationData").Single().Element("Item")!);
    }

    [Fact]
    public void CarVariations_EditedSiren_IsPatched_WindowsPreserved()
    {
        var data = _svc.LoadCarVariations(Data("carvariations.meta"));
        data.SirenSettings = 43;
        _svc.SaveCarVariations(data, Temp("carvariations.meta"));

        var item = XDocument.Load(Temp("carvariations.meta")).Descendants("variationData").Single().Element("Item")!;
        Assert.Equal("43", item.Element("sirenSettings")!.Attribute("value")!.Value);
        Assert.Equal("window_lf", item.Element("windowsWithExposedEdges")!.Element("Item")!.Value);
    }

    // ---------------- carcols.meta ----------------

    [Fact]
    public void CarCols_UnchangedRoundTrip_IsIdentical()
    {
        var data = _svc.LoadCarCols(Data("carcols.meta"));
        _svc.SaveCarCols(data, Temp("carcols.meta"));

        AssertSameXml(
            XDocument.Load(Data("carcols.meta")).Descendants("Sirens").Single(),
            XDocument.Load(Temp("carcols.meta")).Descendants("Sirens").Single());
    }

    [Fact]
    public void CarCols_EditedName_IsPatched()
    {
        var data = _svc.LoadCarCols(Data("carcols.meta"));
        data.SirenSettings[0].Name = "renamed";
        data.SirenSettings[0].Sirens[0].Color = "0xFFFF0000";
        _svc.SaveCarCols(data, Temp("carcols.meta"));

        var item = XDocument.Load(Temp("carcols.meta")).Descendants("Sirens").Single().Element("Item")!;
        Assert.Equal("renamed", item.Element("name")!.Value);
        Assert.Equal("0xFFFF0000", item.Element("sirens")!.Element("Item")!.Element("color")!.Attribute("value")!.Value);
        Assert.Equal("1431655765", item.Element("leftHeadLight")!.Element("sequencer")!.Attribute("value")!.Value);
    }

    // ---------------- project persistence ----------------

    [Fact]
    public void ProjectJson_KeepsSourceXml_AndRelationships()
    {
        var vehicle = new Vehicle
        {
            Name = "jvpolice",
            Handling = _svc.LoadAllHandlings(Data("handling.meta"))["JVPOLICE"],
            VehicleMeta = _svc.LoadAllVehicleMetas(Data("vehicles.meta"))["jvpolice"],
            CarVariation = _svc.LoadCarVariations(Data("carvariations.meta")),
        };
        vehicle.VehicleMeta.DisplayName = "Police Cruiser";

        var json = JsonSerializer.Serialize(vehicle, ProjectJsonOptions.Default);
        var restored = JsonSerializer.Deserialize<Vehicle>(json, ProjectJsonOptions.Default)!;

        Assert.Equal(vehicle.Handling.SourceXml, restored.Handling!.SourceXml);
        Assert.Equal(vehicle.VehicleMeta.SourceXml, restored.VehicleMeta!.SourceXml);
        Assert.Equal(vehicle.CarVariation.SourceXml, restored.CarVariation!.SourceXml);
        Assert.Equal("Police Cruiser", restored.VehicleMeta.DisplayName);
        Assert.Equal(2, restored.VehicleMeta.TxdRelationships.Count);

        // Exported XML after a project save/load cycle is still identical to the original
        _svc.SaveHandling(restored.Handling, Temp("handling.meta"));
        AssertSameXml(HandlingItem(Data("handling.meta")), HandlingItem(Temp("handling.meta")));
    }
}
