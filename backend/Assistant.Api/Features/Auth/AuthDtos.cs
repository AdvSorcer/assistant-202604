namespace Assistant.Api.Features.Auth;

public sealed record LoginRequest(string Password);

public sealed record AuthResponse(string Token);
