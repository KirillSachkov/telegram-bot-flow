using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

public sealed class ErrorHandlingMiddleware : IUpdateMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IUpdateResponder _responder;
    private readonly string _errorMessage;

    public ErrorHandlingMiddleware(
        ILogger<ErrorHandlingMiddleware> logger,
        IUpdateResponder responder,
        IOptions<BotConfiguration> config)
    {
        _logger = logger;
        _responder = responder;
        _errorMessage = config.Value.ErrorMessage;
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

    private async Task TryNotifyUser(UpdateContext context)
    {
        try
        {
            if (context.ChatId != 0)
            {
                await _responder.ReplyAsync(context, _errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send error notification to user {UserId}", context.UserId);
        }
    }
}
