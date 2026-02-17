using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Pipeline;

public interface IUpdateMiddleware
{
    Task InvokeAsync(UpdateContext context, UpdateDelegate next);
}
