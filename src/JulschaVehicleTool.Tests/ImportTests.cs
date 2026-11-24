using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Services;
using JulschaVehicleTool.Core.Services.Import;

namespace JulschaVehicleTool.Tests;

public class ImportTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "jvt_import_" + Guid.NewGuid().ToString("N"));
    private readonly string _pack;
    private readonly string _projectDir;
    private readonly MetaXmlService _meta = new();

    public ImportTests()
    {
        _pack = Path.Combine(_root, "pack");
        _projectDir = Path.Combine(_root, "project");
        BuildPack();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private static string Data(string file) => Path.Combine(AppContext.BaseDirectory, "TestData", file);

    /// <summary>A typical downloaded pack: stream/ + data/ with oddly named metas.</summary>
    private void BuildPack()
    {
        var stream = Path.Combine(_pack, "stream");
        Directory.CreateDirectory(stream);
        foreach (var f in new[]
                 {
                     "jvpolice.yft", "jvpolice_hi.yft", "jvpolice.ytd", "jvpolice+hi.ytd",
                     "jvpolice_bumf.yft", "jvpolice_spoiler.yft", "jvpolice_livery.ytd",
                     "jvambulance.yft", "jvambulance.ytd",
                     // no metas at all for this one
                     "mycar.yft", "mycar.ytd", "mycar_wing.yft",
                 })
            File.WriteAllText(Path.Combine(stream, f), f);

        // Nested folder + hidden folder that must be ignored
        Directory.CreateDirectory(Path.Combine(_pack, "stream", "extra"));
        File.WriteAllText(Path.Combine(_pack, "stream", "extra", "random_prop.ydr"), "x");
        Directory.CreateDirectory(Path.Combine(_pack, ".git"));
        File.WriteAllText(Path.Combine(_pack, ".git", "jvpolice.yft"), "hidden");

        var data = Path.Combine(_pack, "data", "jvpack");
        Directory.CreateDirectory(data);
        File.Copy(Data("vehicles.meta"), Path.Combine(data, "vehicles_jv.meta"));
        File.Copy(Data("handling.meta"), Path.Combine(data, "handling_police.meta"));
        File.Copy(Data("carvariations.meta"), Path.Combine(data, "carvariations.meta"));
        File.Copy(Data("carcols.meta"), Path.Combine(data, "sirens.meta"));
        File.WriteAllText(Path.Combine(_pack, "fxmanifest.lua"), "fx_version 'cerulean'");
    }

    private ImportScan Scan() => new VehicleImportScanner(_meta).Scan(_pack);

    private static VehicleCandidate Get(ImportScan scan, string name) => scan.Vehicles.Single(v => v.Name == name);

    // ---------------- scanner ----------------

    [Fact]
    public void Scan_GroupsMainSlotsModPartsAndExtraTextures()
    {
        var police = Get(Scan(), "jvpolice");

        Assert.EndsWith("jvpolice.yft", police.Model);
        Assert.EndsWith("jvpolice_hi.yft", police.ModelHi);
        Assert.EndsWith("jvpolice.ytd", police.Texture);
        Assert.EndsWith("jvpolice+hi.ytd", police.TextureHi);
        Assert.Equal(["jvpolice_bumf.yft", "jvpolice_spoiler.yft"], police.ModParts.Select(Path.GetFileName));
        Assert.Equal(["jvpolice_livery.ytd"], police.ExtraTextures.Select(Path.GetFileName));
        Assert.True(police.Include);
    }

    [Fact]
    public void Scan_DetectsMetasByRootElement_AndAttachesThem()
    {
        var scan = Scan();
        Assert.Contains(scan.MetaFiles, m => m.Kind == MetaKind.Vehicles && m.Path.EndsWith("vehicles_jv.meta"));
        Assert.Contains(scan.MetaFiles, m => m.Kind == MetaKind.CarCols && m.Path.EndsWith("sirens.meta"));

        var police = Get(scan, "jvpolice");
        Assert.Equal("JVPOLICE", police.VehicleMeta?.GameName);
        Assert.Equal("JVPOLICE", police.Handling?.HandlingName);   // via handlingId
        Assert.Equal(42, police.CarVariation?.SirenSettings);
        Assert.Single(scan.CarCols.SirenSettings);

        var ambulance = Get(scan, "jvambulance");
        Assert.NotNull(ambulance.VehicleMeta);
        Assert.Null(ambulance.Handling);
        Assert.Contains(ambulance.Notes, n => n.Contains("JVAMBULANCE"));
    }

    [Fact]
    public void Scan_VehicleWithoutMetas_IsDetectedFromFiles()
    {
        var scan = Scan();
        var mycar = Get(scan, "mycar");
        Assert.EndsWith("mycar.yft", mycar.Model);
        Assert.Equal(["mycar_wing.yft"], mycar.ModParts.Select(Path.GetFileName));
        Assert.Null(mycar.VehicleMeta);

        // mod parts never become vehicles of their own
        Assert.DoesNotContain(scan.Vehicles, v => v.Name is "jvpolice_bumf" or "mycar_wing");
        Assert.Equal(3, scan.Vehicles.Count);
    }

    [Fact]
    public void Scan_IgnoresHiddenFolders_AndReportsUnassigned()
    {
        var scan = Scan();
        Assert.DoesNotContain(scan.Warnings, w => w.Contains(".git"));
        Assert.Empty(scan.UnassignedFiles); // .ydr is not a vehicle stream file type
    }

    [Fact]
    public void Scan_LongestVehicleNameClaimsItsParts()
    {
        var dir = Path.Combine(_root, "prefix");
        Directory.CreateDirectory(dir);
        foreach (var f in new[] { "police.yft", "police.ytd", "police_bike.yft", "police_bike.ytd", "police_bike_bumf.yft", "police_bumf.yft" })
            File.WriteAllText(Path.Combine(dir, f), f);

        var scan = new VehicleImportScanner(_meta).Scan(dir);
        Assert.Equal(["police_bike_bumf.yft"], Get(scan, "police_bike").ModParts.Select(Path.GetFileName));
        Assert.Equal(["police_bumf.yft"], Get(scan, "police").ModParts.Select(Path.GetFileName));
    }

    // ---------------- import ----------------

    private (Project project, Resource resource, ProjectService service) NewProject()
    {
        var service = new ProjectService(new NoEncryption());
        var project = service.CreateNew(_projectDir);
        var resource = new Resource { Name = "jvpack" };
        project.Resources.Add(resource);
        return (project, resource, service);
    }

    [Fact]
    public void Import_CopiesAllFiles_AppliesMetas_AndReferencedSirens()
    {
        var (project, resource, service) = NewProject();
        var result = service.ImportScan(project, resource, Scan());

        Assert.Empty(result.Warnings);
        Assert.Equal(3, resource.Vehicles.Count);

        var police = resource.Vehicles.Single(v => v.Name == "jvpolice");
        Assert.Equal(3, police.ExtraFiles.Count);
        Assert.False(police.HasMissingFiles(project.FolderPath));
        Assert.Equal("JVPOLICE", police.Handling!.HandlingName);
        Assert.NotNull(police.Handling.SourceXml);
        Assert.Null(police.CarCols);

        Assert.Equal([42], project.CarCols.SirenSettings.Select(s => s.Id));
        Assert.True(project.IsDirty);
    }

    [Fact]
    public void Import_UnselectedVehicles_AreSkipped_ExistingNamesAreNotOverwritten()
    {
        var (project, resource, service) = NewProject();
        var scan = Scan();
        Get(scan, "mycar").Include = false;
        service.ImportScan(project, resource, scan);
        Assert.Equal(2, resource.Vehicles.Count);

        var second = service.ImportScan(project, resource, Scan());
        Assert.Single(second.ImportedVehicles); // only mycar is new
        Assert.Equal(2, second.Warnings.Count(w => w.StartsWith("Skipped")));
        Assert.Equal(3, resource.Vehicles.Count);
    }

    [Fact]
    public void Export_IncludesModPartsInStream()
    {
        var (project, resource, service) = NewProject();
        service.ImportScan(project, resource, Scan());

        var output = Path.Combine(_root, "export");
        var result = new FiveMExportService().ExportResource(new FiveMExportOptions
        {
            Resource = resource, Project = project, OutputPath = output,
            MetaXmlService = _meta, ProjectService = service,
        });

        Assert.True(result.Success, result.Error);
        Assert.DoesNotContain(result.Warnings, w => w.StartsWith("COLLISION") || w.StartsWith("Missing"));
        foreach (var f in new[] { "jvpolice.yft", "jvpolice_bumf.yft", "jvpolice_livery.ytd", "mycar_wing.yft" })
            Assert.True(File.Exists(Path.Combine(output, "stream", f)), f);
    }

    [Fact]
    public void ImportVehicleFiles_ModPartGoesToExtraFiles_NotMainSlot()
    {
        var (project, resource, service) = NewProject();
        var vehicle = VehicleDefaults.CreateDefault("jvpolice");
        resource.Vehicles.Add(vehicle);

        var stream = Path.Combine(_pack, "stream");
        service.ImportVehicleFiles(project, vehicle,
            [Path.Combine(stream, "jvpolice.yft"), Path.Combine(stream, "jvpolice_bumf.yft")]);

        Assert.Equal(Path.Combine("jvpolice", "jvpolice.yft"), vehicle.YftRelativePath);
        Assert.Equal([Path.Combine("jvpolice", "jvpolice_bumf.yft")], vehicle.ExtraFiles);
    }

    // ---------------- meta auto-match ----------------

    private static List<Vehicle> Vehicles(params string[] names) =>
        names.Select(n => new Vehicle { Name = n }).ToList();

    [Fact]
    public void AutoMatch_DoesNotConfuse_Police1_With_Police10()
    {
        var matches = new MetaImportMatchService().AutoMatch(
            new Dictionary<string, int> { ["police1"] = 1 }, Vehicles("police10"));
        Assert.Null(matches.Single().MatchedVehicle);
    }

    [Fact]
    public void AutoMatch_ExactWins_AndEachVehicleIsUsedOnce()
    {
        var vehicles = Vehicles("police1", "police1_lspd");
        var matches = new MetaImportMatchService().AutoMatch(
            new Dictionary<string, int> { ["police1_lspd"] = 1, ["police1"] = 2 }, vehicles);

        Assert.Equal("police1_lspd", matches.Single(m => m.ParsedName == "police1_lspd").MatchedVehicle!.Name);
        Assert.Equal("police1", matches.Single(m => m.ParsedName == "police1").MatchedVehicle!.Name);
        Assert.All(matches, m => Assert.True(m.IsAutoApplicable));
    }

    [Fact]
    public void AutoMatch_PartialMatch_IsOnlyASuggestion()
    {
        var matches = new MetaImportMatchService().AutoMatch(
            new Dictionary<string, int> { ["police1"] = 1 }, Vehicles("police1_lspd"));
        var m = matches.Single();
        Assert.Equal(MatchConfidence.Partial, m.Confidence);
        Assert.False(m.IsAutoApplicable);
    }

    [Fact]
    public void AutoMatch_MatchesViaHandlingName()
    {
        var v = new Vehicle { Name = "my_police", Handling = new HandlingData { HandlingName = "JVPOLICE" } };
        var m = new MetaImportMatchService().AutoMatch(new Dictionary<string, int> { ["JVPOLICE"] = 1 }, [v]).Single();
        Assert.Same(v, m.MatchedVehicle);
        Assert.Equal(MatchConfidence.High, m.Confidence);
    }
}

internal sealed class NoEncryption : IEncryptionService
{
    public byte[] EncryptLocal(byte[] plaintext) => plaintext;
    public byte[] DecryptLocal(byte[] ciphertext) => ciphertext;
    public byte[] EncryptWithPassword(byte[] plaintext, string password) => plaintext;
    public byte[] DecryptWithPassword(byte[] ciphertext, string password) => ciphertext;
}
