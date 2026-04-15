using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

internal sealed class SessionMiddleware : IUpdateMiddleware
{
    private readonly ISessionStore _sessionStore;
    private readonly ISessionLockProvider _lockProvider;

    public SessionMiddleware(ISessionStore sessionStore, ISessionLockProvider lockProvider)
    {
        _sessionStore = sessionStore;
        _lockProvider = lockProvider;
    }

    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        if (context.UserId == 0)
        {
            await next(context);
            return;
        }

        using IDisposable sessionLock = await _lockProvider.AcquireLockAsync(context.UserId, context.CancellationToken);

        UserSession session = await _sessionStore.GetOrCreateAsync(context.UserId, context.CancellationToken);
        context.Session = session;

        await next(context);

        await _sessionStore.SaveAsync(session, context.CancellationToken);
    }
}