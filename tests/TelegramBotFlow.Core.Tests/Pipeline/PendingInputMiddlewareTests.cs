using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Sessions;
using UpdateContext = TelegramBotFlow.Core.Context.UpdateContext;

namespace TelegramBotFlow.Core.Tests.Pipeline;

public sealed class PendingInputMiddlewareTests
{
    private readonly InputHandlerRegistry _registry;
    private readonly PendingInputMiddleware _middleware;

    public PendingInputMiddlewareTests()
    {
        _registry = new InputHandlerRegistry();
        _middleware = new PendingInputMiddleware(_registry, NullLogger<PendingInputMiddleware>.Instance);
    }

    private static UserSession CreateSession(long userId = 123)
    {
        return new UserSession(userId);
    }

    [Fact]
    public async Task Callback_query_passes_to_next_without_checking_pending()
    {
        UpdateContext context = TestHelpers.CreateCallbackContext("some:callback");
        var session = CreateSession();
        session.Navigation.SetPending("some-action");
        context.Session = session;

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        // Pending should remain unchanged — middleware doesn't touch it for callbacks
        Assert.Equal("some-action", context.Session!.Navigation.PendingInputActionId);
    }

    [Fact]
    public async Task Command_message_clears_pending_and_passes_to_next()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("/start");
        var session = CreateSession();
        session.Navigation.SetPending("some-action");
        context.Session = session;

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Null(context.Session!.Navigation.PendingInputActionId);
    }

    [Fact]
    public async Task Text_message_with_registered_handler_invokes_handler_next_not_called()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("some user text");
        var session = CreateSession();
        session.Navigation.SetPending("my-action");
        context.Session = session;

        bool handlerCalled = false;
        bool nextCalled = false;

        _registry.Register("my-action", _ =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(handlerCalled);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Text_message_with_no_pending_passes_to_next()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("some text");
        var session = CreateSession();
        // No pending set
        context.Session = session;

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Text_message_with_pending_but_unregistered_handler_clears_pending_and_passes_to_next()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("some text");
        var session = CreateSession();
        session.Navigation.SetPending("unknown-action");
        context.Session = session;

        // "unknown-action" not registered in registry

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Null(context.Session!.Navigation.PendingInputActionId);
    }
}
