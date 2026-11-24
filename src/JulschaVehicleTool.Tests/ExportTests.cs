using System.Xml.Linq;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Services;
using JulschaVehicleTool.Core.Services.Import;

namespace JulschaVehicleTool.Tests;

/// <summary>Mod kits / lights, pass-through metas (vehiclelayouts), manifest and output folder handling.</summary>
public class ExportTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "jvt_p4_" + Guid.NewGuid().ToString("N"));
    private readonly string _pack;
    private readonly string _out;
    private readonly MetaXmlService _meta = new();
    private readonly ProjectService _service = new(new NoEncryption());
    private readonly Project _project;
    private readonly Resource _resource;

    public ExportTests()
    {
        _pack = Path.Combine(_root, "pack");
        _out = Path.Combine(_root, "out", "pack");
        var stream = Path.Combine(_pack, "stream");
        Directory.CreateDirectory(stream);
        foreach (var f in new[] { "jvpolice.yft", "jvpolice.ytd", "jvpolice_bumf.yft", "jvambulance.yft", "jvambulance.ytd" })
            File.WriteAllText(Path.Combine(stream, f), f);
        var data = Path.Combine(_pack, "data");
        Directory.CreateDirectory(data);
        foreach (var f in new[] { "vehicles.meta", "handling.meta", "carvariations.meta", "carcols.meta", "vehiclelayouts.meta" })
            File.Copy(Path.Combine(AppContext.BaseDirectory, "TestData", f), Path.Combine(data, f));

        _project = _service.CreateNew(Path.Combine(_root, "project"));
        _resource = new Resource { Name = "pack", Description = "JV emergency pack" };
        _project.Resources.Add(_resource);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private ImportResult Import() =>
        _service.ImportScan(_project, _resource, new VehicleImportScanner(_meta).Scan(_pack));

    private ExportResult Export() => new FiveMExportService().ExportResource(new FiveMExportOptions
    {
        Resource = _resource, Project = _project, OutputPath = _out,
        MetaXmlService = _meta, ProjectService = _service,
    });

    // ---------------- carcols: kits & lights ----------------

    [Fact]
    public void CarCols_KitsAndLights_RoundTripUnchanged()
    {
        var src = Path.Combine(AppContext.BaseDirectory, "TestData", "carcols.meta");
        var data = _meta.LoadCarCols(src);
        Assert.Equal(["1001_jvpolice_modkit", "1002_unused_modkit"], data.Kits.Select(k => k.Name));
        Assert.Equal(1001, data.Kits[0].Id);
        Assert.Equal(1, data.Lights.Single().Id);

        var file = Path.Combine(_root, "carcols.meta");
        Directory.CreateDirectory(_root);
        _meta.SaveCarCols(data, file);

        var expected = XDocument.Load(src).Root!;
        var actual = XDocument.Load(file).Root!;
        foreach (var name in new[] { "Kits", "Lights", "Sirens" })
            Assert.True(XNode.DeepEquals(expected.Element(name), actual.Element(name)), name);
    }

    [Fact]
    public void Import_TakesReferencedKitsLightsAndLayouts()
    {
        var result = Import();
        Assert.Empty(result.Warnings);

        Assert.Equal(["1001_jvpolice_modkit"], _project.CarCols.Kits.Select(k => k.Name)); // unused kit not taken
        Assert.Equal([1], _project.CarCols.Lights.Select(l => l.Id));
        var layout = Assert.Single(_resource.ExtraMetas);
        Assert.Equal("VEHICLE_LAYOUTS_FILE", layout.DataFileType);
        Assert.Contains("LAYOUT_STD_HIGHWINDOW_JV", layout.Content);
    }

    [Fact]
    public void Import_Twice_DoesNotDuplicateKitsLightsOrLayouts()
    {
        Import();
        var scan = new VehicleImportScanner(_meta).Scan(_pack);
        ImportConflicts.Annotate(_project, scan);
        var second = _service.ImportScan(_project, _resource, scan);

        Assert.Single(_project.CarCols.Kits);
        Assert.Single(_project.CarCols.Lights);
        Assert.Single(_resource.ExtraMetas);
        Assert.Empty(second.Warnings);
    }

    [Fact]
    public void Import_KitIdTakenByOtherKit_IsReported()
    {
        _project.CarCols.Kits.Add(new CarColsEntry { Id = 1001, Name = "1001_someone_else", Xml = "<Item><kitName>1001_someone_else</kitName><id value=\"1001\" /></Item>" });
        var result = Import();
        Assert.Contains(result.Warnings, w => w.Contains("1001_jvpolice_modkit") && w.Contains("1001_someone_else"));
    }

    // ---------------- export ----------------

    [Fact]
    public void Export_WritesKitsLightsSirens_Layouts_AndManifestInRightOrder()
    {
        Import();
        var result = Export();
        Assert.True(result.Success, result.Error);

        var carcols = XDocument.Load(Path.Combine(_out, "data", "carcols.meta")).Root!;
        Assert.Equal("1001_jvpolice_modkit", carcols.Element("Kits")!.Element("Item")!.Element("kitName")!.Value);
        Assert.Equal("VMT_BUMPER_F", carcols.Descendants("type").First().Value);
        Assert.Single(carcols.Element("Lights")!.Elements("Item"));
        Assert.Single(carcols.Element("Sirens")!.Elements("Item"));

        Assert.True(File.Exists(Path.Combine(_out, "data", "extra", "vehiclelayouts.meta")));

        var manifest = File.ReadAllText(Path.Combine(_out, "fxmanifest.lua"));
        Assert.Contains("description 'JV emergency pack'", manifest);
        Assert.Contains("data_file 'VEHICLE_LAYOUTS_FILE' 'data/extra/vehiclelayouts.meta'", manifest);
        int Pos(string s) => manifest.IndexOf(s, StringComparison.Ordinal);
        Assert.True(Pos("VEHICLE_LAYOUTS_FILE") < Pos("VEHICLE_METADATA_FILE"));
        Assert.True(Pos("CARCOLS_FILE") < Pos("VEHICLE_VARIATION_FILE"));
    }

    [Fact]
    public void Export_Again_RemovesFilesOfDeletedVehicles_KeepsOwnFiles()
    {
        Import();
        Assert.True(Export().Success);
        File.WriteAllText(Path.Combine(_out, "my_script.lua"), "-- own file");
        Assert.True(File.Exists(Path.Combine(_out, "stream", "jvambulance.yft")));

        _resource.Vehicles.Remove(_resource.Vehicles.Single(v => v.Name == "jvambulance"));
        var result = Export();

        Assert.True(result.Success);
        Assert.False(File.Exists(Path.Combine(_out, "stream", "jvambulance.yft")));
        Assert.False(Directory.Exists(Path.Combine(_out, "data", "jvambulance")));
        Assert.True(File.Exists(Path.Combine(_out, "stream", "jvpolice.yft")));
        Assert.True(File.Exists(Path.Combine(_out, "my_script.lua")));
    }

    [Fact]
    public void Export_IntoForeignFolder_DoesNotDeleteAnything()
    {
        Import();
        var foreignStream = Path.Combine(_out, "stream");
        Directory.CreateDirectory(foreignStream);
        File.WriteAllText(Path.Combine(foreignStream, "someone_elses.yft"), "x");

        var result = Export();
        Assert.True(result.Success);
        Assert.True(File.Exists(Path.Combine(foreignStream, "someone_elses.yft")));
        Assert.Contains(result.Warnings, w => w.Contains("not created by this tool"));
    }

    [Fact]
    public void ExportSingleMeta_Carcols_UsesProjectPool()
    {
        Import();
        var police = _resource.Vehicles.Single(v => v.Name == "jvpolice");
        var file = Path.Combine(_root, "single_carcols.meta");
        new FiveMExportService().ExportSingleMeta(_project, police, "carcols", file, _meta);

        var root = XDocument.Load(file).Root!;
        Assert.Single(root.Element("Sirens")!.Elements("Item"));
        Assert.Single(root.Element("Kits")!.Elements("Item"));
    }
}
