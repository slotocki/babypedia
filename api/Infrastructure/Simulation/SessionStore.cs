using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace BabyRadnik.Infrastructure.Simulation;

public sealed class SessionStore(TimeProvider time)
{
    private const int MaxSessions = 10_000;
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);

    private readonly ConcurrentDictionary<string, SimSession> _sessions = new(StringComparer.Ordinal);

    public string Create(string personaId)
    {
        Purge();
        var id = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        _sessions[id] = new SimSession(personaId, time.GetUtcNow());
        return id;
    }

    public string? FindPersonaId(string? sessionId)
    {
        if (string.IsNullOrEmpty(sessionId) || !_sessions.TryGetValue(sessionId, out var session))
        {
            return null;
        }

        if (time.GetUtcNow() - session.CreatedAt > Lifetime)
        {
            _sessions.TryRemove(sessionId, out _);
            return null;
        }

        return session.PersonaId;
    }

    public IReadOnlyCollection<SimSession> Snapshot() => _sessions.Values.ToList();

    private void Purge()
    {
        if (_sessions.Count < MaxSessions)
        {
            return;
        }

        var now = time.GetUtcNow();
        foreach (var (key, session) in _sessions)
        {
            if (now - session.CreatedAt > Lifetime)
            {
                _sessions.TryRemove(key, out _);
            }
        }

        if (_sessions.Count >= MaxSessions)
        {
            foreach (var key in _sessions.OrderBy(s => s.Value.CreatedAt).Take(MaxSessions / 10).Select(s => s.Key).ToList())
            {
                _sessions.TryRemove(key, out _);
            }
        }
    }
}

public sealed record SimSession(string PersonaId, DateTimeOffset CreatedAt);
