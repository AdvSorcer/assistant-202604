using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Assistant.Api.Features.Auth;

public sealed class SessionStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _sessions = new();
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);

    public string Create()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _sessions[token] = DateTimeOffset.UtcNow.Add(Lifetime);
        return token;
    }

    public bool IsValid(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || !_sessions.TryGetValue(token, out var expiresAt))
        {
            return false;
        }

        if (expiresAt < DateTimeOffset.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return false;
        }

        return true;
    }

    public void Revoke(string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            _sessions.TryRemove(token, out _);
        }
    }
}
