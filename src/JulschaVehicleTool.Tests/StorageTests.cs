using System.Security.Cryptography;
using System.Text;
using JulschaVehicleTool.Core.Models;
using JulschaVehicleTool.Core.Services;

namespace JulschaVehicleTool.Tests;

/// <summary>Optional local encryption, legacy projects, atomic save with backup, password crypto.</summary>
public class StorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "jvt_p5_" + Guid.NewGuid().ToString("N"));
    private readonly string _projectDir;
    private readonly string _yft;
    private readonly byte[] _yftBytes = Encoding.ASCII.GetBytes("RSC7....fake model data");

    public StorageTests()
    {
        _projectDir = Path.Combine(_root, "project");
        Directory.CreateDirectory(_root);
        _yft = Path.Combine(_root, "jvpolice.yft");
        File.WriteAllBytes(_yft, _yftBytes);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private string ProjectFile => Path.Combine(_projectDir, "project.julveh");

    private (ProjectService service, Project project, Vehicle vehicle) NewProject(IEncryptionService? enc = null)
    {
        var service = new ProjectService(enc ?? new FakeDpapi());
        var project = service.CreateNew(_projectDir);
        var resource = new Resource { Name = "pack" };
        project.Resources.Add(resource);
        var vehicle = VehicleDefaults.CreateDefault("jvpolice");
        resource.Vehicles.Add(vehicle);
        service.ImportVehicleFiles(project, vehicle, [_yft]);
        service.Save(project);
        return (service, project, vehicle);
    }

    private string StoredYft => Path.Combine(_projectDir, "vehicles", "jvpolice", "jvpolice.yft");

    // ---------------- optional encryption ----------------

    [Fact]
    public void NewProject_IsStoredUnencrypted()
    {
        NewProject();
        Assert.Equal((byte)'{', File.ReadAllBytes(ProjectFile)[0]);
        Assert.Equal(_yftBytes, File.ReadAllBytes(StoredYft));
    }

    [Fact]
    public void SetLocalEncryption_ConvertsAllFiles_BothWays()
    {
        var (service, project, vehicle) = NewProject();

        service.SetLocalEncryption(project, true);
        Assert.True(ProjectService.IsDpapiBlob(File.ReadAllBytes(ProjectFile)));
        Assert.True(ProjectService.IsDpapiBlob(File.ReadAllBytes(StoredYft)));
        Assert.Equal(_yftBytes, service.DecryptVehicleFile(project, vehicle, vehicle.YftRelativePath!));

        var reopened = service.Open(_projectDir);
        Assert.True(reopened.EncryptLocal);
        Assert.Single(reopened.Resources);

        service.SetLocalEncryption(reopened, false);
        Assert.Equal((byte)'{', File.ReadAllBytes(ProjectFile)[0]);
        Assert.Equal(_yftBytes, File.ReadAllBytes(StoredYft));
        Assert.False(service.Open(_projectDir).EncryptLocal);
    }

    [Fact]
    public void LegacyEncryptedProject_OpensAndStaysEncrypted()
    {
        var enc = new FakeDpapi();
        var (service, project, vehicle) = NewProject(enc);

        // Simulate a project written by 1.0: everything DPAPI, no EncryptLocal flag in the JSON
        var json = File.ReadAllText(ProjectFile).Replace("\"encryptLocal\": false,", "");
        Assert.DoesNotContain("encryptLocal", json);
        File.WriteAllBytes(ProjectFile, enc.EncryptLocal(Encoding.UTF8.GetBytes(json)));
        File.WriteAllBytes(StoredYft, enc.EncryptLocal(_yftBytes));

        var opened = service.Open(_projectDir);
        Assert.True(opened.EncryptLocal);
        Assert.Equal(_yftBytes, service.DecryptVehicleFile(opened, vehicle, vehicle.YftRelativePath!));

        service.Save(opened);
        Assert.True(ProjectService.IsDpapiBlob(File.ReadAllBytes(ProjectFile)));
    }

    [Fact]
    public void MixedProject_ReadsPlainAndEncryptedFiles()
    {
        var enc = new FakeDpapi();
        var (service, project, vehicle) = NewProject(enc);
        File.WriteAllBytes(StoredYft, enc.EncryptLocal(_yftBytes)); // one encrypted file in a plain project
        Assert.Equal(_yftBytes, service.DecryptVehicleFile(project, vehicle, vehicle.YftRelativePath!));
    }

    // ---------------- atomic save + backup ----------------

    [Fact]
    public void Save_KeepsBackup_AndDamagedFileFallsBackToIt()
    {
        var (service, project, _) = NewProject();
        project.Name = "second save";
        service.Save(project);

        Assert.True(File.Exists(ProjectFile + ".bak"));
        Assert.False(File.Exists(ProjectFile + ".tmp"));

        // Crash while writing → garbage in the project file
        File.WriteAllText(ProjectFile, "{ broken");
        var opened = service.Open(_projectDir);

        Assert.True(opened.LoadedFromBackup);
        Assert.True(opened.IsDirty);
        Assert.Single(opened.Resources);
    }

    [Fact]
    public void Open_WithoutBackup_ReportsTheError()
    {
        var (service, _, _) = NewProject();
        File.Delete(ProjectFile + ".bak");
        File.WriteAllText(ProjectFile, "{ broken");
        Assert.ThrowsAny<Exception>(() => service.Open(_projectDir));
    }

    // ---------------- password encryption (ZIP sharing) ----------------

    [Fact]
    public void Password_RoundTrip_UsesV2Format_AndSharesSaltWithinExport()
    {
        var enc = new EncryptionService();
        var a = enc.EncryptWithPassword([1, 2, 3], "secret");
        var b = enc.EncryptWithPassword([4, 5, 6], "secret");

        Assert.Equal("JVE2"u8.ToArray(), a[..4]);
        Assert.Equal(EncryptionService.Pbkdf2Iterations, BitConverter.ToInt32(a, 4));
        Assert.Equal(a[8..24], b[8..24]);          // same salt → key derived once
        Assert.NotEqual(a[24..36], b[24..36]);     // different nonce per file
        Assert.Equal([1, 2, 3], enc.DecryptWithPassword(a, "secret"));

        enc.ClearPasswordCache();
        var c = enc.EncryptWithPassword([1], "secret");
        Assert.NotEqual(a[8..24], c[8..24]);       // new export → new salt
        Assert.ThrowsAny<CryptographicException>(() => new EncryptionService().DecryptWithPassword(a, "wrong"));
    }

    [Fact]
    public void Password_LegacyFormat_IsStillReadable()
    {
        // 1.0 format: salt | nonce | tag | ciphertext, 100k iterations
        var salt = RandomNumberGenerator.GetBytes(16);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var key = Rfc2898DeriveBytes.Pbkdf2("pw", salt, 100_000, HashAlgorithmName.SHA256, 32);
        var plain = Encoding.UTF8.GetBytes("legacy");
        var ct = new byte[plain.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(key, 16)) aes.Encrypt(nonce, plain, ct, tag);

        var blob = salt.Concat(nonce).Concat(tag).Concat(ct).ToArray();
        Assert.Equal(plain, new EncryptionService().DecryptWithPassword(blob, "pw"));
    }

    [Fact]
    public void ProjectZip_RoundTrip_WithRealPasswordEncryption()
    {
        var (service, project, _) = NewProject(new EncryptionService());
        var zip = Path.Combine(_root, "share.zip");
        service.ExportProjectZip(project, zip, "pw");

        var target = Path.Combine(_root, "imported");
        var imported = service.ImportProjectZip(zip, "pw", target);
        var v = imported.Resources.Single().Vehicles.Single();

        Assert.Equal(_yftBytes, service.DecryptVehicleFile(imported, v, v.YftRelativePath!));
        Assert.Equal((byte)'{', File.ReadAllBytes(Path.Combine(target, "project.julveh"))[0]);
    }
}

/// <summary>Stand-in for DPAPI on non-Windows: prefixes data with the real DPAPI blob header.</summary>
internal sealed class FakeDpapi : IEncryptionService
{
    private static readonly byte[] Header =
        [0x01, 0x00, 0x00, 0x00, 0xD0, 0x8C, 0x9D, 0xDF, 0x01, 0x15, 0xD1, 0x11, 0x8C, 0x7A, 0x00, 0xC0, 0x4F, 0xC2, 0x97, 0xEB];

    public byte[] EncryptLocal(byte[] plaintext) => Header.Concat(plaintext.Select(b => (byte)(b ^ 0x5A))).ToArray();

    public byte[] DecryptLocal(byte[] ciphertext)
    {
        if (!ciphertext.AsSpan(0, Header.Length).SequenceEqual(Header)) throw new CryptographicException("not a blob");
        return ciphertext.Skip(Header.Length).Select(b => (byte)(b ^ 0x5A)).ToArray();
    }

    public byte[] EncryptWithPassword(byte[] plaintext, string password) => plaintext;
    public byte[] DecryptWithPassword(byte[] ciphertext, string password) => ciphertext;
}
