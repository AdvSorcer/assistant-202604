using Assistant.Api.Models;

namespace Assistant.Api.Features.Logs;

public sealed record DailyLogRequest(DateOnly Date, string Content);

public sealed record DailyLogResponse(
    int Id,
    DateOnly Date,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public static class LogMapping
{
    public static DailyLogResponse ToDailyLogResponse(this DailyLog log) => new(
        log.Id,
        log.Date,
        log.Content,
        log.CreatedAt,
        log.UpdatedAt);
}
