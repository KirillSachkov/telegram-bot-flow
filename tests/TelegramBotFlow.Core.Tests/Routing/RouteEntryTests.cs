using FluentAssertions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Routing;

namespace TelegramBotFlow.Core.Tests.Routing;

public sealed class RouteEntryTests
{
    private static readonly UpdateDelegate NoOp = _ => Task.CompletedTask;

    [Theory]
    [InlineData("/start", "/start", true)]
    [InlineData("/start", "/START", true)]
    [InlineData("/start", "/start@MyBot", true)]
    [InlineData("/start", "/help", false)]
    [InlineData("/start", "start", false)]
    [InlineData("/start", "hello world", false)]
    public void Command_MatchesCorrectly(string routeCommand, string messageText, bool expected)
    {
        var route = RouteEntry.Command(routeCommand, NoOp);
        UpdateContext ctx = TestHelpers.CreateMessageContext(messageText);

        _ = route.Matches(ctx).Should().Be(expected);
    }

    [Fact]
    public void Command_WithArguments_Matches()
    {
        var route = RouteEntry.Command("/start", NoOp);
        UpdateContext ctx = TestHelpers.CreateMessageContext("/start some arguments");

        _ = route.Matches(ctx).Should().BeTrue();
    }

    [Theory]
    [InlineData("profile", "profile", true)]
    [InlineData("profile", "settings", false)]
    [InlineData("action:*", "action:edit", true)]
    [InlineData("action:*", "action:delete", true)]
    [InlineData("action:*", "other:edit", false)]
    [InlineData("menu:*", "menu:", true)]
    [InlineData("exact", "exact", true)]
    [InlineData("exact", "exact_extra", false)]
    public void Callback_MatchesCorrectly(string routePattern, string callbackData, bool expected)
    {
        var route = RouteEntry.Callback(routePattern, NoOp);
        UpdateContext ctx = TestHelpers.CreateCallbackContext(callbackData);

        _ = route.Matches(ctx).Should().Be(expected);
    }

    [Fact]
    public void Message_WithPredicate_MatchesCorrectly()
    {
        var route = RouteEntry.Message(
            ctx => ctx.MessageText?.Contains("привет") == true,
            NoOp);

        UpdateContext matchCtx = TestHelpers.CreateMessageContext("привет мир");
        UpdateContext noMatchCtx = TestHelpers.CreateMessageContext("до свидания");

        _ = route.Matches(matchCtx).Should().BeTrue();
        _ = route.Matches(noMatchCtx).Should().BeFalse();
    }

    [Fact]
    public void Command_DoesNotMatchCallbackUpdate()
    {
        var route = RouteEntry.Command("/start", NoOp);
        UpdateContext ctx = TestHelpers.CreateCallbackContext("start");

        _ = route.Matches(ctx).Should().BeFalse();
    }

    [Fact]
    public void Message_WithScreenPredicate_MatchesOnlyWhenScreenAndTextMatch()
    {
        var route = RouteEntry.Message(
            ctx => ctx.Screen == "settings:main" && ctx.MessageText == "Язык",
            NoOp);

        // Correct screen + correct text = match
        UpdateContext matchCtx = TestHelpers.CreateMessageContext("Язык");
        matchCtx.Session = new Core.Sessions.UserSession(1)
        {
            CurrentScreen = "settings:main"
        };
        _ = route.Matches(matchCtx).Should().BeTrue();

        // Wrong screen + correct text = no match
        UpdateContext wrongScreenCtx = TestHelpers.CreateMessageContext("Язык");
        wrongScreenCtx.Session = new Core.Sessions.UserSession(2)
        {
            CurrentScreen = "settings:lang"
        };
        _ = route.Matches(wrongScreenCtx).Should().BeFalse();

        // Correct screen + wrong text = no match
        UpdateContext wrongTextCtx = TestHelpers.CreateMessageContext("Назад");
        wrongTextCtx.Session = new Core.Sessions.UserSession(3)
        {
            CurrentScreen = "settings:main"
        };
        _ = route.Matches(wrongTextCtx).Should().BeFalse();

        // No session (Screen = null) + correct text = no match
        UpdateContext noSessionCtx = TestHelpers.CreateMessageContext("Язык");
        _ = route.Matches(noSessionCtx).Should().BeFalse();
    }
}
