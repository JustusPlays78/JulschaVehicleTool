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
}

[SupportedOSPlatform("windows")]
public class EncryptionService : IEncryptionService
{
    private const int SaltSize = 16;
    private const int NonceSize = 12; // AES-GCM standard
    private const int TagSize = 16;   // AES-GCM standard
    private const int KeySize = 32;   // AES-256
    private const int Pbkdf2Iterations = 100_000;

    public byte[] EncryptLocal(byte[] plaintext)
    {
        return ProtectedData.Protect(plaintext, null, DataProtectionScope.CurrentUser);
    }

    public byte[] DecryptLocal(byte[] ciphertext)
    {
        return ProtectedData.Unprotect(ciphertext, null, DataProtectionScope.CurrentUser);
    }

    public byte[] EncryptWithPassword(byte[] plaintext, string password)
    {
        // Generate random salt and nonce
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);

        // Derive key from password using PBKDF2
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, KeySize);

        // Encrypt with AES-256-GCM
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // Output format: [salt (16)] [nonce (12)] [tag (16)] [ciphertext (N)]
        var result = new byte[SaltSize + NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
        Buffer.BlockCopy(nonce, 0, result, SaltSize, NonceSize);
        Buffer.BlockCopy(tag, 0, result, SaltSize + NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, SaltSize + NonceSize + TagSize, ciphertext.Length);

        return result;
    }

    public byte[] DecryptWithPassword(byte[] encrypted, string password)
    {
        if (encrypted.Length < SaltSize + NonceSize + TagSize)
            throw new CryptographicException("Encrypted data is too short.");

        // Extract components
        var salt = new byte[SaltSize];
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var ciphertextLength = encrypted.Length - SaltSize - NonceSize - TagSize;
        var ciphertext = new byte[ciphertextLength];

        Buffer.BlockCopy(encrypted, 0, salt, 0, SaltSize);
        Buffer.BlockCopy(encrypted, SaltSize, nonce, 0, NonceSize);
        Buffer.BlockCopy(encrypted, SaltSize + NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(encrypted, SaltSize + NonceSize + TagSize, ciphertext, 0, ciphertextLength);

        // Derive key from password
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, KeySize);

        // Decrypt with AES-256-GCM
        var plaintext = new byte[ciphertextLength];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }
}
