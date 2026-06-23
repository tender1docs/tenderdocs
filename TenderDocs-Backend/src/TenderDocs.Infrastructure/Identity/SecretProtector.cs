using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Infrastructure.Identity;

/// <summary>
/// AES-256-CBC encryption for secrets at rest (storage credentials JSON blob).
/// Key derived from Encryption:Key in configuration. Output format: base64(iv).base64(cipher).
/// </summary>
public class SecretProtector : ISecretProtector
{
    private readonly byte[] _key;

    public SecretProtector(IConfiguration config)
    {
        var raw = config["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key is not configured.");
        // Derive a stable 32-byte key from whatever the operator supplied.
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
    }

    public string Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        using var enc = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var cipher = enc.TransformFinalBlock(bytes, 0, bytes.Length);
        return $"{Convert.ToBase64String(aes.IV)}.{Convert.ToBase64String(cipher)}";
    }

    public string Decrypt(string ciphertext)
    {
        var parts = ciphertext.Split('.', 2);
        if (parts.Length != 2) throw new FormatException("Malformed ciphertext.");
        var iv = Convert.FromBase64String(parts[0]);
        var cipher = Convert.FromBase64String(parts[1]);
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        using var dec = aes.CreateDecryptor();
        var plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }
}
