using System.Collections.Concurrent;

namespace TelegramBotFlow.Core.Sessions;

public sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<long, UserSession> _sessions = new();

    public Task<UserSession> GetOrCreateAsync(long userId, CancellationToken cancellationToken = default)
    {
        UserSession session = _sessions.GetOrAdd(userId, id => new UserSession(id));
        session.LastActivity = DateTime.UtcNow;

        return Task.FromResult(session);
    }

    public Task SaveAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.UserId] = session;
        return Task.CompletedTask;
    }
}
