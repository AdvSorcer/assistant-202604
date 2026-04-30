using Assistant.Api.Infrastructure;
using Assistant.Api.Models;
using Assistant.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Assistant.Api.Features.Settings;

public static class SettingsEndpoints
{
    public const string OpenRouterApiKeySetting = "ai.openrouter.apiKey";
    public const string OpenRouterModelSetting = "ai.openrouter.model";
    public const string SecurityAdminPasswordHashSetting = "security.adminPasswordSha256";
    public const string SecurityEncryptionKeySetting = "security.encryptionKey";
    public const string DefaultOpenRouterModel = "minimax/minimax-m2.7";
    public const string DefaultAdminPassword = "admin";

    public static RouteGroupBuilder MapSettingsEndpoints(this RouteGroupBuilder api)
    {
        var settingsApi = api.MapGroup("/settings");

        settingsApi.MapGet("/ai", async (AssistantDbContext db) =>
        {
            var model = await GetSettingValue(db, OpenRouterModelSetting) ?? DefaultOpenRouterModel;
            var hasApiKey = !string.IsNullOrWhiteSpace(await GetSettingValue(db, OpenRouterApiKeySetting));
            return Results.Ok(new AiSettingsResponse("OpenRouter", model, hasApiKey));
        });

        settingsApi.MapPut("/ai", async (AiSettingsRequest request, AssistantDbContext db, IPasswordCipher cipher) =>
        {
            var model = string.IsNullOrWhiteSpace(request.Model) ? DefaultOpenRouterModel : request.Model.Trim();
            await UpsertSetting(db, OpenRouterModelSetting, model);

            if (!string.IsNullOrWhiteSpace(request.ApiKey))
            {
                await UpsertSetting(db, OpenRouterApiKeySetting, cipher.Encrypt(request.ApiKey.Trim()));
            }

            await db.SaveChangesAsync();
            var hasApiKey = !string.IsNullOrWhiteSpace(await GetSettingValue(db, OpenRouterApiKeySetting));
            return Results.Ok(new AiSettingsResponse("OpenRouter", model, hasApiKey));
        });

        settingsApi.MapGet("/security", async (AssistantDbContext db, IConfiguration configuration) =>
        {
            var hasAdminPassword = !string.IsNullOrWhiteSpace(await GetSettingValue(db, SecurityAdminPasswordHashSetting));
            var hasEncryptionKey = EncryptionKeyStore.HasKey(configuration);

            return Results.Ok(new SecuritySettingsResponse(hasAdminPassword, hasEncryptionKey));
        });

        settingsApi.MapPut("/security", async (SecuritySettingsRequest request, AssistantDbContext db, IConfiguration configuration, IPasswordCipher cipher) =>
        {
            var adminPassword = request.AdminPassword?.Trim();
            if (!string.IsNullOrWhiteSpace(adminPassword))
            {
                await UpsertSetting(db, SecurityAdminPasswordHashSetting, HashPassword(adminPassword));
            }

            if (request.RotateEncryptionKey)
            {
                var oldKey = cipher.GetCurrentKey();
                var newKey = EncryptionKeyStore.GenerateKey();
                await using var transaction = await db.Database.BeginTransactionAsync();
                var reencryptResult = await ReencryptSecrets(db, oldKey, newKey);
                if (reencryptResult is not null)
                {
                    return reencryptResult;
                }

                await db.SaveChangesAsync();
                try
                {
                    EncryptionKeyStore.WriteKey(configuration, newKey);
                    await transaction.CommitAsync();
                }
                catch
                {
                    EncryptionKeyStore.WriteKey(configuration, oldKey);
                    throw;
                }

                return Results.Ok(new SecuritySettingsResponse(true, EncryptionKeyStore.HasKey(configuration)));
            }

            await db.SaveChangesAsync();
            return Results.Ok(new SecuritySettingsResponse(true, EncryptionKeyStore.HasKey(configuration)));
        });

        return api;
    }

    public static async Task<string?> GetSettingValue(AssistantDbContext db, string key) =>
        await db.AppSettings
            .AsNoTracking()
            .Where(setting => setting.Key == key)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync();

    public static string? GetSettingValueSync(AssistantDbContext db, string key) =>
        db.AppSettings
            .AsNoTracking()
            .Where(setting => setting.Key == key)
            .Select(setting => setting.Value)
            .FirstOrDefault();

    public static void EnsureSecurityDefaults(AssistantDbContext db, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(GetSettingValueSync(db, SecurityAdminPasswordHashSetting)))
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = SecurityAdminPasswordHashSetting,
                Value = HashPassword(DefaultAdminPassword)
            });
        }

        EncryptionKeyStore.EnsureKeyFile(db, configuration, SecurityEncryptionKeySetting);

        db.SaveChanges();
    }

    public static string HashPassword(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static async Task UpsertSetting(AssistantDbContext db, string key, string? value)
    {
        var setting = await db.AppSettings.FirstOrDefaultAsync(item => item.Key == key);
        if (setting is null)
        {
            db.AppSettings.Add(new AppSetting { Key = key, Value = value });
            return;
        }

        setting.Value = value;
        setting.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static async Task<IResult?> ReencryptSecrets(AssistantDbContext db, byte[] oldKey, byte[] newKey)
    {
        try
        {
            var vms = await db.Vms.Include(vm => vm.Accounts).ToListAsync();
            foreach (var account in vms.SelectMany(vm => vm.Accounts).Where(account => !string.IsNullOrWhiteSpace(account.EncryptedPassword)))
            {
                var password = PasswordCipher.DecryptWithKey(account.EncryptedPassword!, oldKey);
                account.EncryptedPassword = PasswordCipher.EncryptWithKey(password, newKey);
            }

            var apiKeySetting = await db.AppSettings.FirstOrDefaultAsync(setting => setting.Key == OpenRouterApiKeySetting);
            if (!string.IsNullOrWhiteSpace(apiKeySetting?.Value))
            {
                var apiKey = PasswordCipher.DecryptWithKey(apiKeySetting.Value, oldKey);
                apiKeySetting.Value = PasswordCipher.EncryptWithKey(apiKey, newKey);
                apiKeySetting.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
        catch (CryptographicException)
        {
            return Results.BadRequest(new { message = "目前加密金鑰無法解開既有密碼資料，請先確認原金鑰設定" });
        }
        catch (FormatException)
        {
            return Results.BadRequest(new { message = "既有密碼資料格式不正確，無法更換加密金鑰" });
        }

        return null;
    }
}
