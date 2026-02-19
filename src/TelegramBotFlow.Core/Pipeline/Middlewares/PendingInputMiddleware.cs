using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Routing;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

public sealed class PendingInputMiddleware : IUpdateMiddleware
{
    private readonly InputHandlerRegistry _registry;

    public PendingInputMiddleware(InputHandlerRegistry registry)
    {
        _registry = registry;
    }

    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        // Callbacks проходят без изменений — NavigationHandler очищает pending сам
        if (context.UpdateType != UpdateType.Message)
        {
            await next(context);
            return;
        }

        // Команды сбрасывают pending и передают управление роутеру
        if (context.MessageText?.StartsWith('/') == true)
        {
            if (context.Session is not null)
                context.Session.PendingInputActionId = null;

            await next(context);
            return;
        }

        string? actionId = context.Session?.PendingInputActionId;
        if (actionId is null)
        {
            await next(context);
            return;
        }

        UpdateDelegate? handler = _registry.Find(actionId);
        if (handler is null)
        {
            if (context.Session is not null)
                context.Session.PendingInputActionId = null;

            await next(context);
            return;
        }

        await handler(context);
    }
}
