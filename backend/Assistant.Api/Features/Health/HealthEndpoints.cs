namespace Assistant.Api.Features.Health;

public static class HealthEndpoints
{
    public static RouteGroupBuilder MapHealthEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        return api;
    }
}
