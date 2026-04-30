using System.Security.Cryptography;
using Assistant.Api.Infrastructure;
using Microsoft.Data.Sqlite;

namespace Assistant.Api.Services;

public static class EncryptionKeyStore
{
    private const string SecretsDirectoryName = "secrets";
    private const string EncryptionKeyFileName = "encryption.key";

    public static string GetKeyPath(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=./data/assistant.db";
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        var dataDirectory = Path.GetDirectoryName(Path.GetFullPath(dataSource)) ?? AppContext.BaseDirectory;

        return Path.Combine(dataDirectory, SecretsDirectoryName, EncryptionKeyFileName);
    }

    public static bool HasKey(IConfiguration configuration) => File.Exists(GetKeyPath(configuration));

    public static byte[] ReadKey(IConfiguration configuration)
    {
        var keyPath = GetKeyPath(configuration);
        if (!File.Exists(keyPath))
        {
            throw new InvalidOperationException($"Security encryption key file is missing: {keyPath}");
        }

        var value = File.ReadAllText(keyPath).Trim();
        if (PasswordCipher.TryParseKey(value, out var key))
        {
            return key;
        }

        throw new InvalidOperationException($"Security encryption key file is invalid: {keyPath}");
    }

    public static byte[] GenerateKey() => RandomNumberGenerator.GetBytes(32);

    public static void WriteKey(IConfiguration configuration, byte[] key)
    {
        if (key.Length != 32)
        {
            throw new InvalidOperationException("Security encryption key must be 32 bytes.");
        }

        var keyPath = GetKeyPath(configuration);
        var directory = Path.GetDirectoryName(keyPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(keyPath, Convert.ToBase64String(key));
        RestrictFilePermissions(keyPath);
    }

    public static void EnsureKeyFile(AssistantDbContext db, IConfiguration configuration, string legacySettingKey)
    {
        var legacySetting = db.AppSettings.FirstOrDefault(setting => setting.Key == legacySettingKey);
        if (!HasKey(configuration))
        {
            if (PasswordCipher.TryParseKey(legacySetting?.Value, out var legacyKey))
            {
                WriteKey(configuration, legacyKey);
            }
            else
            {
                WriteKey(configuration, GenerateKey());
            }
        }

        if (legacySetting is not null)
        {
            db.AppSettings.Remove(legacySetting);
        }
    }

    private static void RestrictFilePermissions(string keyPath)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
