using System.Text.Json.Serialization;

namespace Assistant.Api.Features.AiWeeklyReports;

public sealed record AiWeeklyReportRequest(DateOnly StartDate, DateOnly EndDate);

public sealed record AiWeeklyReportResponse(
    string Report,
    int LogsCount,
    string Model,
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record OpenRouterChatRequest(
    string Model,
    List<OpenRouterMessage> Messages,
    double Temperature,
    [property: JsonPropertyName("max_tokens")]
    int MaxTokens);

public sealed record OpenRouterMessage(string Role, string Content);

public sealed record OpenRouterChatResponse(List<OpenRouterChoice>? Choices);

public sealed record OpenRouterChoice(OpenRouterResponseMessage? Message);

public sealed record OpenRouterResponseMessage(string? Role, string? Content);
