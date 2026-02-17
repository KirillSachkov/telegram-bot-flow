namespace TelegramBotFlow.Core.Sessions;

public interface ISessionStore
{
    Task<UserSession> GetOrCreateAsync(long userId, CancellationToken cancellationToken = default);

    Task SaveAsync(UserSession session, CancellationToken cancellationToken = default);
}
