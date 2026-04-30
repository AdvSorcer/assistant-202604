using System.Security.Cryptography;
using System.Text;
using Assistant.Api.Features.Settings;
using Assistant.Api.Infrastructure;

namespace Assistant.Api.Features.Auth;

public static class AuthEndpoints
{
    public const string AuthLoginRateLimitPolicy = "auth-login";

    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder api)
    {
        var authApi = api.MapGroup("/auth");

        authApi.MapPost("/login", async (LoginRequest request, AssistantDbContext db, SessionStore sessions) =>
        {
            var storedPasswordHash = await SettingsEndpoints.GetSettingValue(db, SettingsEndpoints.SecurityAdminPasswordHashSetting);

            var isValid = !string.IsNullOrWhiteSpace(storedPasswordHash) &&
                FixedTimeEqualsNormalizedHex(SettingsEndpoints.HashPassword(request.Password), storedPasswordHash);

            if (!isValid)
            {
                return Results.Unauthorized();
            }

            var token = sessions.Create();
            return Results.Ok(new AuthResponse(token));
        }).RequireRateLimiting(AuthLoginRateLimitPolicy);

        authApi.MapPost("/logout", (HttpRequest request, SessionStore sessions) =>
        {
            var token = GetBearerToken(request);
            sessions.Revoke(token);
            return Results.NoContent();
        });

        authApi.MapGet("/me", (HttpRequest request, SessionStore sessions) =>
        {
            var token = GetBearerToken(request);
            return sessions.IsValid(token) ? Results.Ok(new { authenticated = true }) : Results.Unauthorized();
        });

        return api;
    }

    public static IApplicationBuilder UseApiSessionAuth(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Path.StartsWithSegments("/api/health") ||
                context.Request.Path.StartsWithSegments("/api/auth"))
            {
                await next();
                return;
            }

            var token = GetBearerToken(context.Request);
            var sessions = context.RequestServices.GetRequiredService<SessionStore>();
            if (string.IsNullOrWhiteSpace(token) || !sessions.IsValid(token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "請先登入" });
                return;
            }

            await next();
        });
    }

    private static string GetBearerToken(HttpRequest request)
    {
        return request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static bool FixedTimeEqualsNormalizedHex(string left, string right)
    {
        return FixedTimeEquals(left.ToLowerInvariant(), right.ToLowerInvariant());
    }
}
