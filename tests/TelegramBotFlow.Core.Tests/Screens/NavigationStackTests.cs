using FluentAssertions;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Screens;

public sealed class NavigationStackTests
{
    [Fact]
    public void PushScreen_FirstScreen_SetsCurrentScreen()
    {
        var session = new UserSession(1);

        session.PushScreen("main");

        _ = session.CurrentScreen.Should().Be("main");
        _ = session.NavigationStack.Should().BeEmpty();
    }

    [Fact]
    public void PushScreen_SecondScreen_PushesFirstToStack()
    {
        var session = new UserSession(1);
        session.PushScreen("main");

        session.PushScreen("settings");

        _ = session.CurrentScreen.Should().Be("settings");
        _ = session.NavigationStack.Should().ContainSingle().Which.Should().Be("main");
    }

    [Fact]
    public void PopScreen_ReturnsToLastScreen()
    {
        var session = new UserSession(1);
        session.PushScreen("main");
        session.PushScreen("settings");
        session.PushScreen("lang");

        string? popped = session.PopScreen();

        _ = popped.Should().Be("settings");
        _ = session.CurrentScreen.Should().Be("settings");
        _ = session.NavigationStack.Should().ContainSingle().Which.Should().Be("main");
    }

    [Fact]
    public void PopScreen_EmptyStack_ReturnsNull()
    {
        var session = new UserSession(1);
        session.PushScreen("main");

        string? popped = session.PopScreen();

        _ = popped.Should().BeNull();
        _ = session.CurrentScreen.Should().Be("main");
    }

    [Fact]
    public void PopScreen_WithoutPush_ReturnsNull()
    {
        var session = new UserSession(1);

        string? popped = session.PopScreen();

        _ = popped.Should().BeNull();
    }

    [Fact]
    public void Clear_ResetsNavigationStack()
    {
        var session = new UserSession(1);
        session.PushScreen("main");
        session.PushScreen("settings");
        session.PushScreen("lang");

        session.Clear();

        _ = session.CurrentScreen.Should().BeNull();
        _ = session.NavigationStack.Should().BeEmpty();
        _ = session.NavMessageId.Should().BeNull();
    }

    [Fact]
    public void DeepNavigation_And_BackToRoot()
    {
        var session = new UserSession(1);
        session.PushScreen("main");
        session.PushScreen("settings");
        session.PushScreen("lang");
        session.PushScreen("lang_confirm");

        _ = session.PopScreen().Should().Be("lang");
        _ = session.PopScreen().Should().Be("settings");
        _ = session.PopScreen().Should().Be("main");
        _ = session.PopScreen().Should().BeNull();
        _ = session.CurrentScreen.Should().Be("main");
    }

    [Fact]
    public void PushScreen_DuplicateInStack_TruncatesToExisting()
    {
        var session = new UserSession(1);
        session.PushScreen("main");
        session.PushScreen("profile");
        session.PushScreen("settings");

        session.PushScreen("profile");

        _ = session.CurrentScreen.Should().Be("profile");
        _ = session.NavigationStack.Should().Equal("main");
    }

    [Fact]
    public void PushScreen_SameAsCurrent_NoOp()
    {
        var session = new UserSession(1);
        session.PushScreen("main");
        session.PushScreen("settings");

        session.PushScreen("settings");

        _ = session.CurrentScreen.Should().Be("settings");
        _ = session.NavigationStack.Should().ContainSingle().Which.Should().Be("main");
    }

    [Fact]
    public void PushScreen_ExceedsMaxDepth_DropsOldest()
    {
        var session = new UserSession(1);

        for (int i = 0; i <= UserSession.MAX_NAVIGATION_DEPTH + 5; i++)
            session.PushScreen($"screen_{i}");

        _ = session.NavigationStack.Should().HaveCount(UserSession.MAX_NAVIGATION_DEPTH);
        _ = session.NavigationStack[0].Should().NotBe("screen_0");
    }

    [Fact]
    public void PushScreen_DuplicateOfRoot_ClearsStack()
    {
        var session = new UserSession(1);
        session.PushScreen("main");
        session.PushScreen("a");
        session.PushScreen("b");
        session.PushScreen("c");

        session.PushScreen("main");

        _ = session.CurrentScreen.Should().Be("main");
        _ = session.NavigationStack.Should().BeEmpty();
    }
}
