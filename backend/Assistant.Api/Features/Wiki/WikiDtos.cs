using Assistant.Api.Models;

namespace Assistant.Api.Features.Wiki;

public sealed record WikiPageRequest(string Title, string Slug, string Content);

public sealed record WikiPageResponse(
    int Id,
    string Title,
    string Slug,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public static class WikiMapping
{
    public static WikiPageResponse ToWikiPageResponse(this WikiPage page) => new(
        page.Id,
        page.Title,
        page.Slug,
        page.Content,
        page.CreatedAt,
        page.UpdatedAt);
}
