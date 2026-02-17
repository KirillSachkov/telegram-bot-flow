using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

public sealed class SessionMiddleware : IUpdateMiddleware
{
    private readonly ISessionStore _sessionStore;

    public SessionMiddleware(ISessionStore sessionStore)
    {
        _sessionStore = sessionStore;
    }

    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        if (context.UserId == 0)
        {
            await next(context);
            return;
        }

        var session = await _sessionStore.GetOrCreateAsync(context.UserId, context.CancellationToken);
        context.Session = session;

        await next(context);

        await _sessionStore.SaveAsync(session, context.CancellationToken);
    }
}
