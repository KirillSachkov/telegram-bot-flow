using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Flows;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Flows;

public sealed class FlowManagerTests
{
    private readonly FlowManager _manager = new(NullLogger<FlowManager>.Instance);
    private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
    private readonly IServiceProvider _services = Substitute.For<IServiceProvider>();

    [Fact]
    public async Task StartFlowAsync_SetsSessionStateToFirstStep()
    {
        var flow = new FlowBuilder("register")
            .Ask("name", "Как вас зовут?")
            .OnComplete((ctx, answers) => Task.CompletedTask)
            .Build();

        _manager.Register(flow);

        var session = new UserSession(1);
        var ctx = CreateContext("/register", session);

        await _manager.StartFlowAsync(ctx, "register");

        session.CurrentFlowId.Should().Be("register");
        session.CurrentStepId.Should().Be("name");
        session.IsInFlow.Should().BeTrue();
    }

    [Fact]
    public async Task HandleInputAsync_AdvancesToNextStep()
    {
        var flow = new FlowBuilder("reg")
            .Ask("name", "Имя?")
            .Ask("email", "Email?")
            .OnComplete((_, _) => Task.CompletedTask)
            .Build();

        _manager.Register(flow);

        var session = new UserSession(1) { CurrentFlowId = "reg", CurrentStepId = "name" };
        var ctx = CreateContext("John", session);

        var handled = await _manager.HandleInputAsync(ctx);

        handled.Should().BeTrue();
        session.CurrentStepId.Should().Be("email");
        session.GetString("flow:reg:name").Should().Be("John");
    }

    [Fact]
    public async Task HandleInputAsync_ValidationFails_StaysOnSameStep()
    {
        var flow = new FlowBuilder("reg")
            .Ask("email", "Email?", validate: Validators.Email)
            .OnComplete((_, _) => Task.CompletedTask)
            .Build();

        _manager.Register(flow);

        var session = new UserSession(1) { CurrentFlowId = "reg", CurrentStepId = "email" };
        var ctx = CreateContext("not-an-email", session);

        var handled = await _manager.HandleInputAsync(ctx);

        handled.Should().BeTrue();
        session.CurrentStepId.Should().Be("email");
    }

    [Fact]
    public async Task HandleInputAsync_LastStep_CompletesFlowAndCallsOnComplete()
    {
        Dictionary<string, string>? completedAnswers = null;

        var flow = new FlowBuilder("reg")
            .Ask("name", "Имя?")
            .OnComplete((ctx, answers) =>
            {
                completedAnswers = new(answers);
                return Task.CompletedTask;
            })
            .Build();

        _manager.Register(flow);

        var session = new UserSession(1) { CurrentFlowId = "reg", CurrentStepId = "name" };
        var ctx = CreateContext("John", session);

        await _manager.HandleInputAsync(ctx);

        session.IsInFlow.Should().BeFalse();
        completedAnswers.Should().NotBeNull();
        completedAnswers!["name"].Should().Be("John");
    }

    [Fact]
    public async Task HandleInputAsync_NotInFlow_ReturnsFalse()
    {
        var session = new UserSession(1);
        var ctx = CreateContext("hello", session);

        var handled = await _manager.HandleInputAsync(ctx);

        handled.Should().BeFalse();
    }

    [Fact]
    public void CancelFlow_ClearsFlowState()
    {
        var session = new UserSession(1) { CurrentFlowId = "reg", CurrentStepId = "step1" };
        var ctx = CreateContext("text", session);

        _manager.CancelFlow(ctx);

        session.IsInFlow.Should().BeFalse();
        session.CurrentFlowId.Should().BeNull();
        session.CurrentStepId.Should().BeNull();
    }

    [Fact]
    public async Task MultiStepFlow_CollectsAllAnswers()
    {
        Dictionary<string, string>? result = null;

        var flow = new FlowBuilder("survey")
            .Ask("q1", "Question 1?")
            .Ask("q2", "Question 2?")
            .Ask("q3", "Question 3?")
            .OnComplete((ctx, answers) =>
            {
                result = new(answers);
                return Task.CompletedTask;
            })
            .Build();

        _manager.Register(flow);

        var session = new UserSession(1) { CurrentFlowId = "survey", CurrentStepId = "q1" };

        await _manager.HandleInputAsync(CreateContext("Answer 1", session));
        session.CurrentStepId.Should().Be("q2");

        await _manager.HandleInputAsync(CreateContext("Answer 2", session));
        session.CurrentStepId.Should().Be("q3");

        await _manager.HandleInputAsync(CreateContext("Answer 3", session));
        session.IsInFlow.Should().BeFalse();

        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result!["q1"].Should().Be("Answer 1");
        result["q2"].Should().Be("Answer 2");
        result["q3"].Should().Be("Answer 3");
    }

    private UpdateContext CreateContext(string text, UserSession session)
    {
        var update = new Update
        {
            Message = new Message
            {
                Text = text,
                From = new User { Id = session.UserId, FirstName = "Test" },
                Chat = new Chat { Id = 456, Type = ChatType.Private },
                Date = DateTime.UtcNow,
                Id = 1
            }
        };

        var ctx = new UpdateContext(update, _bot, _services);
        ctx.Session = session;
        return ctx;
    }
}
