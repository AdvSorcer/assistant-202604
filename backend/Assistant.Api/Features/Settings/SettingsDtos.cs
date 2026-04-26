namespace Assistant.Api.Features.Settings;

public sealed record AiSettingsResponse(string Provider, string Model, bool HasApiKey);

public sealed record AiSettingsRequest(string Model, string? ApiKey);
