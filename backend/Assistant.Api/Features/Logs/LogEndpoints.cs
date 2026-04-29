using Assistant.Api.Infrastructure;
using Assistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Assistant.Api.Features.Logs;

public static class LogEndpoints
{
    public static RouteGroupBuilder MapLogEndpoints(this RouteGroupBuilder api)
    {
        var logApi = api.MapGroup("/logs");

        logApi.MapGet("/", async (AssistantDbContext db) =>
            await db.DailyLogs
                .AsNoTracking()
                .OrderByDescending(log => log.Date)
                .Select(log => log.ToDailyLogResponse())
                .ToListAsync());

        logApi.MapPost("/", async (DailyLogRequest request, AssistantDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Results.BadRequest(new { message = "請輸入日誌內容" });
            }

            if (await db.DailyLogs.AnyAsync(log => log.Date == request.Date))
            {
                return Results.Conflict(new { message = "同一天已經有日誌" });
            }

            var log = new DailyLog { Date = request.Date, Content = request.Content };
            db.DailyLogs.Add(log);
            await db.SaveChangesAsync();
            return Results.Created($"/api/logs/{log.Id}", log.ToDailyLogResponse());
        });

        logApi.MapPut("/{id:int}", async (int id, DailyLogRequest request, AssistantDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Results.BadRequest(new { message = "請輸入日誌內容" });
            }

            if (await db.DailyLogs.AnyAsync(log => log.Id != id && log.Date == request.Date))
            {
                return Results.Conflict(new { message = "同一天已經有日誌" });
            }

            var log = await db.DailyLogs.FindAsync(id);
            if (log is null)
            {
                return Results.NotFound();
            }

            log.Date = request.Date;
            log.Content = request.Content;
            log.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(log.ToDailyLogResponse());
        });

        logApi.MapDelete("/{id:int}", async (int id, AssistantDbContext db) =>
        {
            var deleted = await db.DailyLogs.Where(log => log.Id == id).ExecuteDeleteAsync();
            return deleted == 0 ? Results.NotFound() : Results.NoContent();
        });

        return api;
    }
}
