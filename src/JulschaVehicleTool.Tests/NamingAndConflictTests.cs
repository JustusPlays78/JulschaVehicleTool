using System.IO.Compression;
using System.Xml.Linq;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Services;
using JulschaVehicleTool.Core.Services.Import;

namespace JulschaVehicleTool.Tests;

/// <summary>Rename, clone, project-wide conflicts, siren IDs and cleanup of unreferenced files.</summary>
public class NamingAndConflictTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "jvt_p3_" + Guid.NewGuid().ToString("N"));
    private readonly string _pack;
    private readonly MetaXmlService _meta = new();
    private readonly ProjectService _service = new(new NoEncryption());
    private readonly Project _project;
    private readonly Resource _resource;

    public NamingAndConflictTests()
    {
        _pack = Path.Combine(_root, "pack");
        var stream = Path.Combine(_pack, "stream");
        Directory.CreateDirectory(stream);
        foreach (var f in new[] { "jvpolice.yft", "jvpolice_hi.yft", "jvpolice.ytd", "jvpolice+hi.ytd", "jvpolice_bumf.yft" })
            File.WriteAllText(Path.Combine(stream, f), f);
        var data = Path.Combine(_pack, "data");
        Directory.CreateDirectory(data);
        foreach (var f in new[] { "vehicles.meta", "handling.meta", "carvariations.meta", "carcols.meta" })
            File.Copy(Path.Combine(AppContext.BaseDirectory, "TestData", f), Path.Combine(data, f));

        _project = _service.CreateNew(Path.Combine(_root, "project"));
        _resource = new Resource { Name = "pack" };
        _project.Resources.Add(_resource);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private string VehiclesDir => Path.Combine(_project.FolderPath!, "vehicles");

    private ImportScan Scan() => new VehicleImportScanner(_meta).Scan(_pack);

    private Vehicle ImportPolice()
    {
        var scan = Scan();
        foreach (var v in scan.Vehicles) v.Include = v.Name == "jvpolice";
        _service.ImportScan(_project, _resource, scan);
        return _resource.Vehicles.Single(v => v.Name == "jvpolice");
    }

    // ---------------- rename ----------------

    [Fact]
    public void Rename_MovesFolder_RenamesFiles_AndUpdatesMetaNames()
    {
        var v = ImportPolice();
        _service.RenameVehicle(_project, v, "jvpolice", "jvpolice2");

        Assert.Equal("jvpolice2", v.Name);
        Assert.Equal(Path.Combine("jvpolice2", "jvpolice2.yft"), v.YftRelativePath);
        Assert.Equal(Path.Combine("jvpolice2", "jvpolice2_hi.yft"), v.YftHiRelativePath);
        Assert.Equal(Path.Combine("jvpolice2", "jvpolice2+hi.ytd"), v.YtdHiRelativePath);
        Assert.Equal([Path.Combine("jvpolice2", "jvpolice2_bumf.yft")], v.ExtraFiles);
        Assert.False(v.HasMissingFiles(_project.FolderPath));
        Assert.False(Directory.Exists(Path.Combine(VehiclesDir, "jvpolice")));

        Assert.Equal("jvpolice2", v.VehicleMeta!.ModelName);
        Assert.Equal("jvpolice2", v.VehicleMeta.TxdName);
        Assert.Equal("JVPOLICE2", v.VehicleMeta.HandlingId);
        Assert.Equal("JVPOLICE2", v.VehicleMeta.GameName);
        Assert.Equal("JVPOLICE2", v.Handling!.HandlingName);
        Assert.Equal("jvpolice2", v.CarVariation!.ModelName);
        Assert.Contains(v.VehicleMeta.TxdRelationships, r => r.Child == "jvpolice2");

        // Exported XML carries the new names, unknown fields still preserved
        var file = Path.Combine(_root, "v.meta");
        _meta.SaveVehicleMeta(v.VehicleMeta, file);
        var item = XDocument.Load(file).Descendants("InitDatas").Single().Element("Item")!;
        Assert.Equal("jvpolice2", item.Element("modelName")!.Value);
        Assert.NotNull(item.Element("lodDistances"));

        Assert.Empty(ProjectValidator.Validate(_project, _resource).Where(i => i.Issue.Severity == "Error"));
    }

    [Fact]
    public void Rename_ToExistingName_Fails_AndChangesNothing()
    {
        var v = ImportPolice();
        var other = VehicleDefaults.CreateDefault("taken");
        _resource.Vehicles.Add(other);
        var before = v.YftRelativePath;

        var ex = Assert.Throws<InvalidOperationException>(() => _service.RenameVehicle(_project, v, "jvpolice", "TAKEN"));
        Assert.Contains("already exists", ex.Message);
        Assert.Equal("jvpolice", v.Name);
        Assert.Equal(before, v.YftRelativePath);
        Assert.True(Directory.Exists(Path.Combine(VehiclesDir, "jvpolice")));
    }

    [Fact]
    public void Rename_WhenTargetFolderExists_RollsBack()
    {
        var v = ImportPolice();
        Directory.CreateDirectory(Path.Combine(VehiclesDir, "ghost"));
        File.WriteAllText(Path.Combine(VehiclesDir, "ghost", "x.txt"), "x");

        Assert.Throws<InvalidOperationException>(() => _service.RenameVehicle(_project, v, "jvpolice", "ghost"));
        Assert.Equal("jvpolice", v.Name);
        Assert.False(v.HasMissingFiles(_project.FolderPath));
    }

    [Theory]
    [InlineData("")]
    [InlineData("with space")]
    [InlineData("umlaut_ä")]
    [InlineData("dot.name")]
    public void Rename_InvalidName_Fails(string name)
    {
        var v = ImportPolice();
        Assert.Throws<InvalidOperationException>(() => _service.RenameVehicle(_project, v, "jvpolice", name));
    }

    [Fact]
    public void Rename_CaseOnly_Works()
    {
        var v = ImportPolice();
        _service.RenameVehicle(_project, v, "jvpolice", "JVPolice");
        Assert.Equal("JVPolice", v.Name);
        Assert.False(v.HasMissingFiles(_project.FolderPath));
    }

    // ---------------- clone ----------------

    [Fact]
    public void Clone_CopiesFilesUnderNewName_AndIsIndependent()
    {
        var v = ImportPolice();
        var name = VehicleNaming.UniqueName(_project, v.Name, "_copy");
        Assert.Equal("jvpolice_copy", name);

        var clone = _service.CloneVehicle(_project, v, name);
        _resource.Vehicles.Add(clone);

        Assert.Equal(Path.Combine("jvpolice_copy", "jvpolice_copy.yft"), clone.YftRelativePath);
        Assert.Equal([Path.Combine("jvpolice_copy", "jvpolice_copy_bumf.yft")], clone.ExtraFiles);
        Assert.False(clone.HasMissingFiles(_project.FolderPath));
        Assert.Equal("jvpolice_copy", clone.VehicleMeta!.ModelName);
        Assert.Equal("JVPOLICE_COPY", clone.Handling!.HandlingName);

        // original untouched
        Assert.Equal("jvpolice", v.VehicleMeta!.ModelName);
        Assert.Equal(Path.Combine("jvpolice", "jvpolice.yft"), v.YftRelativePath);

        // both can be exported side by side
        Assert.False(ProjectValidator.HasErrors(ProjectValidator.Validate(_project, _resource)));
        clone.Handling.FMass = 9999;
        Assert.NotEqual(9999, v.Handling!.FMass);

        Assert.Equal("jvpolice_copy_2", VehicleNaming.UniqueName(_project, v.Name, "_copy"));
    }

    // ---------------- validation ----------------

    [Fact]
    public void Validator_FindsDuplicateModelNamesAcrossResources()
    {
        ImportPolice();
        var otherResource = new Resource { Name = "other" };
        var dup = VehicleDefaults.CreateDefault("something");
        dup.VehicleMeta!.ModelName = "jvpolice";
        otherResource.Vehicles.Add(dup);
        _project.Resources.Add(otherResource);

        var issues = ProjectValidator.Validate(_project, _resource);
        Assert.Contains(issues, i => i.Issue.Severity == "Error" && i.Issue.Message.Contains("something (other)"));
    }

    [Fact]
    public void Validator_ModelNameMustMatchYft_AndSirenRange()
    {
        var v = ImportPolice();
        v.VehicleMeta!.ModelName = "wrong";
        v.CarVariation!.SirenSettings = 300;

        var issues = ProjectValidator.Validate(_project, _resource);
        Assert.Contains(issues, i => i.Issue.Severity == "Error" && i.Issue.Message.Contains("won't spawn"));
        Assert.Contains(issues, i => i.Issue.Severity == "Error" && i.Issue.Message.Contains("outside 1–255"));
    }

    // ---------------- import conflicts ----------------

    [Fact]
    public void Import_SameNameTwice_ProposesNewName_AndRenamesFilesAndMetas()
    {
        ImportPolice();

        var scan = Scan();
        foreach (var c in scan.Vehicles) c.Include = c.Name == "jvpolice";
        ImportConflicts.Annotate(_project, scan);
        var candidate = scan.Vehicles.Single(c => c.Name == "jvpolice");
        Assert.Equal("jvpolice_2", candidate.ImportAs);

        var result = _service.ImportScan(_project, _resource, scan);
        var imported = result.ImportedVehicles.Single();
        Assert.Equal("jvpolice_2", imported.Name);
        Assert.Equal(Path.Combine("jvpolice_2", "jvpolice_2.yft"), imported.YftRelativePath);
        Assert.Equal([Path.Combine("jvpolice_2", "jvpolice_2_bumf.yft")], imported.ExtraFiles);
        Assert.Equal("jvpolice_2", imported.VehicleMeta!.ModelName);
        Assert.Equal("JVPOLICE_2", imported.Handling!.HandlingName);
        Assert.False(ProjectValidator.HasErrors(ProjectValidator.Validate(_project, _resource)));
    }

    [Fact]
    public void Import_SirenIdConflict_IsRenumbered_AndVehicleFollows()
    {
        // Project already has a different siren group with ID 42; IDs 1-3 are reserved on the server
        _project.CarCols.SirenSettings.Add(new SirenSetting { Id = 42, Name = "someone_else" });
        _project.ReservedSirenIds = "1-3";

        var v = ImportPolice();
        var result = _service.ImportScan(_project, _resource, new ImportScan { RootPath = _pack }); // no-op

        Assert.Equal(4, v.CarVariation!.SirenSettings);
        Assert.Contains(_project.CarCols.SirenSettings, s => s.Id == 4 && s.Name == "jvpolice_lightbar");
        Assert.Contains(_project.CarCols.SirenSettings, s => s.Id == 42 && s.Name == "someone_else");

        // The renumbered ID is written to carcols.meta
        var file = Path.Combine(_root, "carcols.meta");
        _meta.SaveCarCols(new CarColsData { SirenSettings = { _project.CarCols.SirenSettings.Single(s => s.Id == 4) } }, file);
        Assert.Equal("4", XDocument.Load(file).Descendants("Sirens").Single().Element("Item")!.Element("id")!.Attribute("value")!.Value);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Import_IdenticalSiren_IsNotDuplicated()
    {
        ImportPolice();
        var scan = Scan();
        foreach (var c in scan.Vehicles) c.Include = c.Name == "jvpolice";
        ImportConflicts.Annotate(_project, scan);
        _service.ImportScan(_project, _resource, scan);

        Assert.Single(_project.CarCols.SirenSettings);
        Assert.All(_resource.Vehicles, v => Assert.Equal(42, v.CarVariation!.SirenSettings));
    }

    [Fact]
    public void SirenIds_ParseListAndNextFree()
    {
        Assert.Equal([1, 2, 3, 99, 254, 255], SirenIds.ParseList("1-3, 99;254-300 abc 0").Order());
        Assert.Equal(4, SirenIds.NextFree(new HashSet<int> { 1, 2, 3 }));
        Assert.Null(SirenIds.NextFree(Enumerable.Range(1, 255).ToHashSet()));
    }

    // ---------------- cleanup ----------------

    [Fact]
    public void Save_DeletesFilesOfRemovedVehicles()
    {
        var v = ImportPolice();
        _service.Save(_project);
        Assert.True(Directory.Exists(Path.Combine(VehiclesDir, "jvpolice")));

        _resource.Vehicles.Remove(v);
        _service.Save(_project);
        Assert.False(Directory.Exists(Path.Combine(VehiclesDir, "jvpolice")));
    }

    [Fact]
    public void Save_DuringImport_DoesNotDeletePendingFiles()
    {
        var scan = Scan();
        var prepared = _service.PrepareImport(_project, scan);
        _service.Save(_project); // e.g. auto-save while the import is running
        _service.ApplyImport(_project, _resource, prepared);

        Assert.All(_resource.Vehicles, v => Assert.False(v.HasMissingFiles(_project.FolderPath)));
    }

    [Fact]
    public void ProjectZip_ContainsOnlyReferencedFiles()
    {
        ImportPolice();
        var orphanDir = Path.Combine(VehiclesDir, "orphan");
        Directory.CreateDirectory(orphanDir);
        File.WriteAllText(Path.Combine(orphanDir, "orphan.yft"), "x");

        var zip = Path.Combine(_root, "p.zip");
        _service.ExportProjectZip(_project, zip, "pw");
        using var archive = ZipFile.OpenRead(zip);
        Assert.DoesNotContain(archive.Entries, e => e.FullName.Contains("orphan"));
        Assert.Contains(archive.Entries, e => e.Name == "jvpolice_bumf.yft");
    }
}
