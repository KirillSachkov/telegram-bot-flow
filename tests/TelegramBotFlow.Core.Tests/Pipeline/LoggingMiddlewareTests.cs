using FluentAssertions;
using Microsoft.Extensions.Logging;
using TelegramBotFlow.Core.Pipeline.Middlewares;

namespace TelegramBotFlow.Core.Tests.Pipeline;

public sealed class LoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_DoesNotLogMessageOrCallbackPayloads()
    {
        const string handoffToken = "handoff_abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG";
        const string callbackToken = "claim_secret-capability";
        var logger = new CapturingLogger<LoggingMiddleware>();
        var middleware = new LoggingMiddleware(logger);

        await middleware.InvokeAsync(
            TestHelpers.CreateMessageContext($"/start\t{handoffToken}"),
            _ => Task.CompletedTask);
        await middleware.InvokeAsync(
            TestHelpers.CreateCallbackContext(callbackToken),
            _ => Task.CompletedTask);

        logger.Messages.Should().NotContain(message => message.Contains(handoffToken, StringComparison.Ordinal));
        logger.Messages.Should().NotContain(message => message.Contains(callbackToken, StringComparison.Ordinal));
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull =>
            null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
