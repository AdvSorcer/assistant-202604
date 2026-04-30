using System.Security.Cryptography;
using System.Text;
namespace Assistant.Api.Services;

public interface IPasswordCipher
{
    string? Encrypt(string? plainText);
    string? Decrypt(string? cipherText);
    byte[] GetCurrentKey();
}

public sealed class PasswordCipher(IConfiguration configuration) : IPasswordCipher
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public string? Encrypt(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return null;
        }

        return EncryptWithKey(plainText, GetCurrentKey());
    }

    public string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
        {
            return null;
        }

        return DecryptWithKey(cipherText, GetCurrentKey());
    }

    public byte[] GetCurrentKey()
    {
        return EncryptionKeyStore.ReadKey(configuration);
    }

    public static string EncryptWithKey(string plainText, byte[] key)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        return Convert.ToBase64String([.. nonce, .. tag, .. cipherBytes]);
    }

    public static string DecryptWithKey(string cipherText, byte[] key)
    {
        var payload = Convert.FromBase64String(cipherText);
        var nonce = payload[..NonceSize];
        var tag = payload[NonceSize..(NonceSize + TagSize)];
        var cipherBytes = payload[(NonceSize + TagSize)..];
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    public static bool TryParseKey(string? value, out byte[] key)
    {
        key = [];
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            var parsedKey = Convert.FromBase64String(value.Trim());
            if (parsedKey.Length != 32)
            {
                return false;
            }

            key = parsedKey;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
