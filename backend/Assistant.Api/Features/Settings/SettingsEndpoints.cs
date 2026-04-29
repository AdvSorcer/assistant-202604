using Assistant.Api.Infrastructure;
using Assistant.Api.Models;
using Assistant.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Assistant.Api.Features.Settings;

public static class SettingsEndpoints
{
    public const string OpenRouterApiKeySetting = "ai.openrouter.apiKey";
    public const string OpenRouterModelSetting = "ai.openrouter.model";
    public const string DefaultOpenRouterModel = "minimax/minimax-m2.7";

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

        return api;
    }

    public static async Task<string?> GetSettingValue(AssistantDbContext db, string key) =>
        await db.AppSettings
            .AsNoTracking()
            .Where(setting => setting.Key == key)
            .Select(setting => setting.Value)
            .FirstOrDefaultAsync();

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
}
