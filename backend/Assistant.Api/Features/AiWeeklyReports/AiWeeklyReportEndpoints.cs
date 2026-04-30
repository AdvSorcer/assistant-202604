using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Assistant.Api.Infrastructure;
using Assistant.Api.Features.Settings;
using Assistant.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Assistant.Api.Features.AiWeeklyReports;

public static class AiWeeklyReportEndpoints
{
    private const string OpenRouterEndpoint = "https://openrouter.ai/api/v1/chat/completions";

    public static RouteGroupBuilder MapAiWeeklyReportEndpoints(this RouteGroupBuilder api)
    {
        var reportApi = api.MapGroup("/ai-weekly-report");

        reportApi.MapPost("/generate", async (
            AiWeeklyReportRequest request,
            AssistantDbContext db,
            IPasswordCipher cipher,
            IHttpClientFactory httpClientFactory,
            CancellationToken cancellationToken) =>
        {
            if (request.EndDate < request.StartDate)
            {
                return Results.BadRequest(new { message = "結束日期不可早於開始日期" });
            }

            var encryptedApiKey = await SettingsEndpoints.GetSettingValue(db, SettingsEndpoints.OpenRouterApiKeySetting);
            var apiKey = cipher.Decrypt(encryptedApiKey);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Results.BadRequest(new { message = "請先到設定頁填寫 OpenRouter API key" });
            }

            var model = await SettingsEndpoints.GetSettingValue(db, SettingsEndpoints.OpenRouterModelSetting)
                ?? SettingsEndpoints.DefaultOpenRouterModel;

            var logs = await db.DailyLogs
                .AsNoTracking()
                .Where(log => log.Date >= request.StartDate && log.Date <= request.EndDate)
                .OrderBy(log => log.Date)
                .ToListAsync(cancellationToken);

            if (logs.Count == 0)
            {
                return Results.BadRequest(new { message = "選取範圍內沒有日誌可產生週報" });
            }

            var prompt = BuildPrompt(request.StartDate, request.EndDate, logs.Select(log => (log.Date, log.Content)));
            var chatRequest = new OpenRouterChatRequest(
                model,
                [
                    new OpenRouterMessage("system", "你是擅長整理工作成果的繁體中文職場週報助理。輸出要清晰、具體、適合直接貼到 Outlook，不要使用 Markdown 表格。"),
                    new OpenRouterMessage("user", prompt),
                ],
                0.2,
                1800);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenRouterEndpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Headers.TryAddWithoutValidation("X-OpenRouter-Title", "Personal Assistant");
            httpRequest.Content = JsonContent.Create(chatRequest, options: new JsonSerializerOptions(JsonSerializerDefaults.Web));

            var httpClient = httpClientFactory.CreateClient();
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Results.Problem(
                    title: "OpenRouter 週報產生失敗",
                    detail: responseBody,
                    statusCode: StatusCodes.Status502BadGateway);
            }

            var chatResponse = JsonSerializer.Deserialize<OpenRouterChatResponse>(responseBody, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var report = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
            if (string.IsNullOrWhiteSpace(report))
            {
                return Results.Problem("OpenRouter 回傳內容為空", statusCode: StatusCodes.Status502BadGateway);
            }

            return Results.Ok(new AiWeeklyReportResponse(report, logs.Count, model, request.StartDate, request.EndDate));
        });

        return api;
    }

    private static string BuildPrompt(DateOnly startDate, DateOnly endDate, IEnumerable<(DateOnly Date, string Content)> logs)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"請根據以下 {startDate:yyyy-MM-dd} 到 {endDate:yyyy-MM-dd} 的工作日誌，整理成可以直接貼到 Outlook 寄給主管的繁體中文週報。");
        builder.AppendLine();
        builder.AppendLine("輸出格式：");
        builder.AppendLine("1. 標題：週報 YYYY/MM/DD-YYYY/MM/DD");
        builder.AppendLine("2. 本週摘要：用 2-4 句整理本週主要工作方向、整體進度與重點結果");
        builder.AppendLine("3. 已完成事項：條列本週已完成或已有明確產出的工作，盡量描述成果與影響");
        builder.AppendLine("4. 進行中事項：條列尚在推進的工作，包含目前狀態、下一步或卡點");
        builder.AppendLine("5. 風險、阻塞與需協助事項：有需要主管協助、決策、協調或注意的事項才列出；沒有就寫「無」");
        builder.AppendLine("6. 下週計畫：根據本週日誌延伸合理下一步，避免過度承諾或捏造新任務");
        builder.AppendLine();
        builder.AppendLine("要求：");
        builder.AppendLine("- 不要捏造日誌沒有提到的具體成果、數字或人名。");
        builder.AppendLine("- 合併重複事項，讓主管能快速掌握重點。");
        builder.AppendLine("- 優先呈現主管需要知道的成果、進度、風險與需要介入的事項。");
        builder.AppendLine("- 語氣專業、簡潔、可直接複製到 Outlook。");
        builder.AppendLine();
        builder.AppendLine("工作日誌：");

        foreach (var (date, content) in logs)
        {
            builder.AppendLine();
            builder.AppendLine($"## {date:yyyy-MM-dd}");
            builder.AppendLine(content.Trim());
        }

        return builder.ToString();
    }
}
