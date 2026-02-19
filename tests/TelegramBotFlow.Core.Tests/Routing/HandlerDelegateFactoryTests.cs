using FluentAssertions;
using NSubstitute;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Routing;

public sealed class HandlerDelegateFactoryTests
{
    // -- Helpers --

    private static IServiceProvider BuildServices(IScreenNavigator? navigator = null)
    {
        var services = Substitute.For<IServiceProvider>();
        var nav = navigator ?? Substitute.For<IScreenNavigator>();
        services.GetService(typeof(IScreenNavigator)).Returns(nav);
        return services;
    }

    private static UpdateContext CreateContext(IServiceProvider? services = null)
        => TestHelpers.CreateMessageContext("test", services: services ?? BuildServices());

    private static UpdateContext CreateCallbackContext(string data, IServiceProvider? services = null)
        => TestHelpers.CreateCallbackContext(data, services: services ?? BuildServices());

    // -- Create: validation --

    [Fact]
    public void Create_Should_Throw_When_Handler_Returns_Task()
    {
        Delegate handler = () => Task.CompletedTask;

        Action act = () => HandlerDelegateFactory.Create(handler);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Task<IEndpointResult>*");
    }

    [Fact]
    public void Create_Should_Throw_When_Handler_Returns_Void()
    {
        Delegate handler = () => { };

        Action act = () => HandlerDelegateFactory.Create(handler);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Task<IEndpointResult>*");
    }

    [Fact]
    public void Create_Should_Not_Throw_When_Handler_Returns_TaskEndpointResult()
    {
        Delegate handler = () => Task.FromResult(BotResults.Empty());

        Action act = () => HandlerDelegateFactory.Create(handler);

        act.Should().NotThrow();
    }

    // -- Create: execution --

    [Fact]
    public async Task Create_Should_Execute_Result_When_Handler_Returns_EndpointResult()
    {
        var navigator = Substitute.For<IScreenNavigator>();
        var services = BuildServices(navigator);
        UpdateContext ctx = CreateContext(services);

        var result = Substitute.For<IEndpointResult>();
        result.KeepPending.Returns(false);

        Delegate handler = (UpdateContext _) => Task.FromResult(result);

        var del = HandlerDelegateFactory.Create(handler);
        await del(ctx);

        await result.Received(1).ExecuteAsync(ctx, navigator);
    }

    [Fact]
    public async Task Create_Should_Inject_Services_Into_Handler_Parameters()
    {
        var services = BuildServices();
        bool serviceCalled = false;

        Delegate handler = (IServiceProvider _) =>
        {
            serviceCalled = true;
            return Task.FromResult(BotResults.Empty());
        };

        // IServiceProvider is itself registered as a service
        services.GetService(typeof(IServiceProvider)).Returns(services);

        var del = HandlerDelegateFactory.Create(handler);
        await del(CreateContext(services));

        serviceCalled.Should().BeTrue();
    }

    // -- CreateForInput: validation --

    [Fact]
    public void CreateForInput_Should_Throw_When_Handler_Returns_Task()
    {
        Delegate handler = () => Task.CompletedTask;

        Action act = () => HandlerDelegateFactory.CreateForInput(handler, "action_id");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Task<IEndpointResult>*");
    }

    [Fact]
    public void CreateForInput_Should_Not_Throw_When_Handler_Returns_TaskEndpointResult()
    {
        Delegate handler = () => Task.FromResult(BotResults.Back());

        Action act = () => HandlerDelegateFactory.CreateForInput(handler, "action_id");

        act.Should().NotThrow();
    }

    // -- CreateForInput: execution --

    [Fact]
    public async Task CreateForInput_Should_Clear_Pending_Before_Invocation()
    {
        var services = BuildServices();
        UpdateContext ctx = CreateContext(services);
        ctx.Session = new UserSession(123) { PendingInputActionId = "old_action" };

        Delegate handler = () => Task.FromResult(BotResults.Empty());

        var del = HandlerDelegateFactory.CreateForInput(handler, "action_id");
        await del(ctx);

        ctx.Session.PendingInputActionId.Should().BeNull();
    }

    [Fact]
    public async Task CreateForInput_Should_Restore_Pending_When_KeepPending_Is_True()
    {
        var services = BuildServices();
        UpdateContext ctx = CreateContext(services);
        ctx.Session = new UserSession(123);

        var result = Substitute.For<IEndpointResult>();
        result.KeepPending.Returns(true);

        Delegate handler = () => Task.FromResult(result);

        var del = HandlerDelegateFactory.CreateForInput(handler, "my_input");
        await del(ctx);

        ctx.Session.PendingInputActionId.Should().Be("my_input");
    }

    [Fact]
    public async Task CreateForInput_Should_Not_Restore_Pending_When_KeepPending_Is_False()
    {
        var services = BuildServices();
        UpdateContext ctx = CreateContext(services);
        ctx.Session = new UserSession(123) { PendingInputActionId = "original" };

        var result = Substitute.For<IEndpointResult>();
        result.KeepPending.Returns(false);

        Delegate handler = () => Task.FromResult(result);

        var del = HandlerDelegateFactory.CreateForInput(handler, "my_input");
        await del(ctx);

        ctx.Session.PendingInputActionId.Should().BeNull();
    }

    [Fact]
    public async Task CreateForInput_Should_Execute_Result()
    {
        var navigator = Substitute.For<IScreenNavigator>();
        var services = BuildServices(navigator);
        UpdateContext ctx = CreateContext(services);

        var result = Substitute.For<IEndpointResult>();
        result.KeepPending.Returns(false);

        Delegate handler = () => Task.FromResult(result);

        var del = HandlerDelegateFactory.CreateForInput(handler, "action_id");
        await del(ctx);

        await result.Received(1).ExecuteAsync(ctx, navigator);
    }

    // -- CreateForCallbackGroup: validation --

    [Fact]
    public void CreateForCallbackGroup_Should_Throw_When_Handler_Returns_Task()
    {
        Delegate handler = (string _) => Task.CompletedTask;

        Action act = () => HandlerDelegateFactory.CreateForCallbackGroup(handler, "prefix");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Task<IEndpointResult>*");
    }

    [Fact]
    public void CreateForCallbackGroup_Should_Not_Throw_When_Handler_Returns_TaskEndpointResult()
    {
        Delegate handler = (string _) => Task.FromResult(BotResults.Empty());

        Action act = () => HandlerDelegateFactory.CreateForCallbackGroup(handler, "prefix");

        act.Should().NotThrow();
    }

    // -- CreateForCallbackGroup: execution --

    [Fact]
    public async Task CreateForCallbackGroup_Should_Pass_Action_Part_To_Handler()
    {
        string? receivedAction = null;

        Delegate handler = (string action) =>
        {
            receivedAction = action;
            return Task.FromResult(BotResults.Empty());
        };

        var del = HandlerDelegateFactory.CreateForCallbackGroup(handler, "nav");
        UpdateContext ctx = CreateCallbackContext("nav:back");

        await del(ctx);

        receivedAction.Should().Be("back");
    }

    [Fact]
    public async Task CreateForCallbackGroup_Should_Execute_Result()
    {
        var navigator = Substitute.For<IScreenNavigator>();
        var services = BuildServices(navigator);
        UpdateContext ctx = CreateCallbackContext("pfx:action", services);

        var result = Substitute.For<IEndpointResult>();
        result.KeepPending.Returns(false);

        Delegate handler = (string _) => Task.FromResult(result);

        var del = HandlerDelegateFactory.CreateForCallbackGroup(handler, "pfx");
        await del(ctx);

        await result.Received(1).ExecuteAsync(ctx, navigator);
    }
}
