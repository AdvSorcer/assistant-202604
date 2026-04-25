using Assistant.Api.Models;

namespace Assistant.Api.Features.Wiki;

public sealed record WikiPageRequest(string Title, string Slug, string Content, bool IsPinned);

public sealed record WikiPageResponse(
    int Id,
    string Title,
    string Slug,
    string Content,
    bool IsPinned,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public static class WikiMapping
{
    public static WikiPageResponse ToWikiPageResponse(this WikiPage page) => new(
        page.Id,
        page.Title,
        page.Slug,
        page.Content,
        page.IsPinned,
        page.CreatedAt,
        page.UpdatedAt);
}
