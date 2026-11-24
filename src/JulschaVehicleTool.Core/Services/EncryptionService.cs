using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace JulschaVehicleTool.Core.Services;

public interface IEncryptionService
{
    /// <summary>
    /// Encrypts data using DPAPI (machine-bound, current user scope).
    /// </summary>
    byte[] EncryptLocal(byte[] plaintext);

    /// <summary>
    /// Decrypts data that was encrypted with EncryptLocal.
    /// </summary>
    byte[] DecryptLocal(byte[] ciphertext);

    /// <summary>
    /// Encrypts data with a password using AES-256-GCM + PBKDF2 key derivation.
    /// Used for portable project ZIP export.
    /// </summary>
    byte[] EncryptWithPassword(byte[] plaintext, string password);

    /// <summary>
    /// Decrypts data that was encrypted with EncryptWithPassword.
    /// </summary>
    byte[] DecryptWithPassword(byte[] ciphertext, string password);

    /// <summary>
    /// Forgets keys derived from passwords (called after a ZIP export/import).
    /// </summary>
    void ClearPasswordCache() { }
}

/// <summary>
/// DPAPI for local storage (Windows only) and AES-256-GCM with PBKDF2-SHA256 for password-protected ZIPs.
/// <para>Password format v2: "JVE2" | iterations (int32 LE) | salt (16) | nonce (12) | tag (16) | ciphertext.
/// Legacy format (no magic): salt | nonce | tag | ciphertext with 100,000 iterations — still readable.</para>
/// <para>Within one export all files share a salt, so the expensive key derivation runs once per export
/// instead of once per file. Every file still gets its own random nonce.</para>
/// </summary>
public class EncryptionService : IEncryptionService
{
    private const int SaltSize = 16;
    private const int NonceSize = 12; // AES-GCM standard
    private const int TagSize = 16;   // AES-GCM standard
    private const int KeySize = 32;   // AES-256
    private const int LegacyIterations = 100_000;
    public const int Pbkdf2Iterations = 600_000; // OWASP 2023 recommendation for PBKDF2-SHA256
    private static readonly byte[] Magic = "JVE2"u8.ToArray();
    private const int HeaderSize = 4 + 4; // magic + iterations

    private readonly Dictionary<string, byte[]> _keyCache = new();
    private readonly Dictionary<string, byte[]> _saltForPassword = new();
    private readonly object _lock = new();

    [SupportedOSPlatform("windows")]
    public byte[] EncryptLocal(byte[] plaintext)
    {
        return ProtectedData.Protect(plaintext, null, DataProtectionScope.CurrentUser);
    }

    [SupportedOSPlatform("windows")]
    public byte[] DecryptLocal(byte[] ciphertext)
    {
        return ProtectedData.Unprotect(ciphertext, null, DataProtectionScope.CurrentUser);
    }

    public byte[] EncryptWithPassword(byte[] plaintext, string password)
    {
        byte[] salt;
        lock (_lock)
        {
            var pwId = PasswordId(password);
            if (!_saltForPassword.TryGetValue(pwId, out salt!))
                _saltForPassword[pwId] = salt = RandomNumberGenerator.GetBytes(SaltSize);
        }

        var key = DeriveKey(password, salt, Pbkdf2Iterations);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(key, TagSize))
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[HeaderSize + SaltSize + NonceSize + TagSize + ciphertext.Length];
        var span = result.AsSpan();
        Magic.CopyTo(span);
        BitConverter.TryWriteBytes(span[4..8], Pbkdf2Iterations);
        salt.CopyTo(span[HeaderSize..]);
        nonce.CopyTo(span[(HeaderSize + SaltSize)..]);
        tag.CopyTo(span[(HeaderSize + SaltSize + NonceSize)..]);
        ciphertext.CopyTo(span[(HeaderSize + SaltSize + NonceSize + TagSize)..]);
        return result;
    }

    public byte[] DecryptWithPassword(byte[] encrypted, string password)
    {
        int offset = 0;
        int iterations = LegacyIterations;

        if (encrypted.Length >= HeaderSize && encrypted.AsSpan(0, 4).SequenceEqual(Magic))
        {
            iterations = BitConverter.ToInt32(encrypted, 4);
            if (iterations is < 1 or > 10_000_000)
                throw new CryptographicException("Invalid iteration count.");
            offset = HeaderSize;
        }

        if (encrypted.Length < offset + SaltSize + NonceSize + TagSize)
            throw new CryptographicException("Encrypted data is too short.");

        var salt = encrypted.AsSpan(offset, SaltSize).ToArray();
        var nonce = encrypted.AsSpan(offset + SaltSize, NonceSize);
        var tag = encrypted.AsSpan(offset + SaltSize + NonceSize, TagSize);
        var ciphertext = encrypted.AsSpan(offset + SaltSize + NonceSize + TagSize);

        var key = DeriveKey(password, salt, iterations);
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    public void ClearPasswordCache()
    {
        lock (_lock)
        {
            foreach (var key in _keyCache.Values) CryptographicOperations.ZeroMemory(key);
            _keyCache.Clear();
            _saltForPassword.Clear();
        }
    }

    private byte[] DeriveKey(string password, byte[] salt, int iterations)
    {
        var cacheKey = $"{PasswordId(password)}:{Convert.ToBase64String(salt)}:{iterations}";
        lock (_lock)
            if (_keyCache.TryGetValue(cacheKey, out var cached))
                return cached;

        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, KeySize);
        lock (_lock)
            _keyCache[cacheKey] = key;
        return key;
    }

    /// <summary>In-memory id for a password (the password itself is not kept as dictionary key).</summary>
    private static string PasswordId(string password) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password)));
}
