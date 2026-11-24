using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Services;

namespace JulschaVehicleTool.Tests;

public class FiveMExportTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "jvt_export_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void VehicleNames_UsesGameNameAsKey_AndDisplayNameAsText()
    {
        var vehicle = VehicleDefaults.CreateDefault("jvpolice");
        vehicle.VehicleMeta!.GameName = "JVPOLICE";
        vehicle.VehicleMeta.DisplayName = "Police Cruiser";

        var resource = new Resource { Name = "pack" };
        resource.Vehicles.Add(vehicle);
        var project = new Project { FolderPath = _tempDir };
        project.Resources.Add(resource);

        var result = new FiveMExportService().ExportResource(new FiveMExportOptions
        {
            Resource = resource,
            Project = project,
            OutputPath = _tempDir,
            MetaXmlService = new MetaXmlService(),
            ProjectService = new ProjectService(new NoEncryption()),
        });

        Assert.True(result.Success, result.Error);
        var lua = File.ReadAllText(Path.Combine(_tempDir, "vehicle_names.lua"));
        Assert.Contains("AddTextEntry('JVPOLICE', 'Police Cruiser')", lua);
    }

    private sealed class NoEncryption : IEncryptionService
    {
        public byte[] EncryptLocal(byte[] plaintext) => plaintext;
        public byte[] DecryptLocal(byte[] ciphertext) => ciphertext;
        public byte[] EncryptWithPassword(byte[] plaintext, string password) => plaintext;
        public byte[] DecryptWithPassword(byte[] ciphertext, string password) => ciphertext;
    }
}
