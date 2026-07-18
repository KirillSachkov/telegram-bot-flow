using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

internal sealed class LoggingMiddleware : IUpdateMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "Processing {UpdateType} from user {UserId} in chat {ChatId}. Command: {Command}, HasText: {HasText}, HasCallback: {HasCallback}",
            context.UpdateType,
            context.UserId,
            context.ChatId,
            GetCommandName(context.MessageText),
            context.MessageText is not null,
            context.CallbackData is not null);

        await next(context);

        sw.Stop();

        _logger.LogInformation(
            "Processed {UpdateType} from user {UserId} in {ElapsedMs}ms",
            context.UpdateType,
            context.UserId,
            sw.ElapsedMilliseconds);
    }

    private static string GetCommandName(string? messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText) || messageText[0] != '/')
            return "(none)";

        int separator = messageText.IndexOfAny([' ', '\t', '\r', '\n']);
        return separator > 0 ? messageText[..separator] : messageText;
    }
}
