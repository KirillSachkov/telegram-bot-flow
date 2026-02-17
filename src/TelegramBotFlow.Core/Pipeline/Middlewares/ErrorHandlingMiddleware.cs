using Microsoft.Extensions.Logging;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

public sealed class ErrorHandlingMiddleware : IUpdateMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(ILogger<ErrorHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Update processing cancelled for user {UserId}", context.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception while processing update {UpdateId} for user {UserId} in chat {ChatId}",
                context.Update.Id,
                context.UserId,
                context.ChatId);

            await TryNotifyUser(context);
        }
    }

    private static async Task TryNotifyUser(UpdateContext context)
    {
        try
        {
            if (context.ChatId != 0)
            {
                await context.ReplyAsync("Произошла ошибка. Попробуйте позже.");
            }
        }
        catch
        {
            // Не позволяем ошибке уведомления сломать обработку
        }
    }
}
