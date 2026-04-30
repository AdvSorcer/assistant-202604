namespace Assistant.Api.Features.Settings;

public sealed record AiSettingsResponse(string Provider, string Model, bool HasApiKey);

public sealed record AiSettingsRequest(string Model, string? ApiKey);

public sealed record SecuritySettingsResponse(bool HasAdminPassword, bool HasEncryptionKey);

public sealed record SecuritySettingsRequest(string? AdminPassword, bool RotateEncryptionKey);
