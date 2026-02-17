using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Flows;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

public sealed class FlowMiddleware : IUpdateMiddleware
{
    private readonly FlowManager _flowManager;

    public FlowMiddleware(FlowManager flowManager)
    {
        _flowManager = flowManager;
    }

    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        if (context.Session?.IsInFlow == true && context.UpdateType == UpdateType.Message)
        {
            var handled = await _flowManager.HandleInputAsync(context);
            if (handled)
                return;
        }

        await next(context);
    }
}
