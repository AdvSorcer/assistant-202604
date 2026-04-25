using System.Security.Cryptography;
using System.Text;

namespace Assistant.Api.Services;

public interface IPasswordCipher
{
    string? Encrypt(string? plainText);
    string? Decrypt(string? cipherText);
}

public sealed class PasswordCipher(IConfiguration configuration, IWebHostEnvironment environment) : IPasswordCipher
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key = ResolveKey(configuration, environment);

    public string? Encrypt(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return null;
        }

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        return Convert.ToBase64String([.. nonce, .. tag, .. cipherBytes]);
    }

    public string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
        {
            return null;
        }

        var payload = Convert.FromBase64String(cipherText);
        var nonce = payload[..NonceSize];
        var tag = payload[NonceSize..(NonceSize + TagSize)];
        var cipherBytes = payload[(NonceSize + TagSize)..];
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] ResolveKey(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredKey = configuration["Security:EncryptionKey"];
        if (!string.IsNullOrWhiteSpace(configuredKey))
        {
            var key = Convert.FromBase64String(configuredKey);
            if (key.Length == 32)
            {
                return key;
            }
        }

        if (environment.IsDevelopment())
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes("development-only-personal-assistant-key"));
        }

        throw new InvalidOperationException("Security:EncryptionKey must be a base64-encoded 32-byte key.");
    }
}
