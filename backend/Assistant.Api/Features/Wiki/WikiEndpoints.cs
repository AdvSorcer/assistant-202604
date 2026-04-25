using Assistant.Api.Data;
using Assistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Assistant.Api.Features.Wiki;

public static class WikiEndpoints
{
    public static RouteGroupBuilder MapWikiEndpoints(this RouteGroupBuilder api)
    {
        var wikiApi = api.MapGroup("/wiki");

        wikiApi.MapGet("/", async (AssistantDbContext db) =>
            await db.WikiPages
                .AsNoTracking()
                .OrderBy(page => page.Title)
                .Select(page => page.ToWikiPageResponse())
                .ToListAsync());

        wikiApi.MapPost("/", async (WikiPageRequest request, AssistantDbContext db) =>
        {
            var validationResult = await ValidateWikiRequest(request, db);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var page = new WikiPage
            {
                Title = request.Title.Trim(),
                Slug = request.Slug.Trim(),
                Content = request.Content
            };
            db.WikiPages.Add(page);
            await db.SaveChangesAsync();
            return Results.Created($"/api/wiki/{page.Id}", page.ToWikiPageResponse());
        });

        wikiApi.MapPut("/{id:int}", async (int id, WikiPageRequest request, AssistantDbContext db) =>
        {
            var validationResult = await ValidateWikiRequest(request, db, id);
            if (validationResult is not null)
            {
                return validationResult;
            }

            var page = await db.WikiPages.FindAsync(id);
            if (page is null)
            {
                return Results.NotFound();
            }

            page.Title = request.Title.Trim();
            page.Slug = request.Slug.Trim();
            page.Content = request.Content;
            page.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(page.ToWikiPageResponse());
        });

        wikiApi.MapDelete("/{id:int}", async (int id, AssistantDbContext db) =>
        {
            var deleted = await db.WikiPages.Where(page => page.Id == id).ExecuteDeleteAsync();
            return deleted == 0 ? Results.NotFound() : Results.NoContent();
        });

        return api;
    }

    private static async Task<IResult?> ValidateWikiRequest(WikiPageRequest request, AssistantDbContext db, int? currentPageId = null)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Slug))
        {
            return Results.BadRequest(new { message = "請輸入文件標題與 Slug" });
        }

        var slug = request.Slug.Trim();
        var slugExists = currentPageId is null
            ? await db.WikiPages.AnyAsync(page => page.Slug == slug)
            : await db.WikiPages.AnyAsync(page => page.Id != currentPageId && page.Slug == slug);

        if (slugExists)
        {
            return Results.Conflict(new { message = "Slug 已存在" });
        }

        return null;
    }
}
