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

        session.CurrentScreen.Should().Be("main");
        session.NavigationStack.Should().BeEmpty();
    }

    [Fact]
    public void PushScreen_SecondScreen_PushesFirstToStack()
    {
        var session = new UserSession(1);
        session.PushScreen("main");

        session.PushScreen("settings");

        session.CurrentScreen.Should().Be("settings");
        session.NavigationStack.Should().ContainSingle().Which.Should().Be("main");
    }

    [Fact]
    public void PopScreen_ReturnsToLastScreen()
    {
        var session = new UserSession(1);
        session.PushScreen("main");
        session.PushScreen("settings");
        session.PushScreen("lang");

        string? popped = session.PopScreen();

        popped.Should().Be("settings");
        session.CurrentScreen.Should().Be("settings");
        session.NavigationStack.Should().ContainSingle().Which.Should().Be("main");
    }

    [Fact]
    public void PopScreen_EmptyStack_ReturnsNull()
    {
        var session = new UserSession(1);
        session.PushScreen("main");

        string? popped = session.PopScreen();

        popped.Should().BeNull();
        session.CurrentScreen.Should().Be("main");
    }

    [Fact]
    public void PopScreen_WithoutPush_ReturnsNull()
    {
        var session = new UserSession(1);

        string? popped = session.PopScreen();

        popped.Should().BeNull();
    }

    [Fact]
    public void Clear_ResetsNavigationStack()
    {
        var session = new UserSession(1);
        session.PushScreen("main");
        session.PushScreen("settings");
        session.PushScreen("lang");

        session.Clear();

        session.CurrentScreen.Should().BeNull();
        session.NavigationStack.Should().BeEmpty();
        session.NavMessageId.Should().BeNull();
    }

    [Fact]
    public void DeepNavigation_And_BackToRoot()
    {
        var session = new UserSession(1);
        session.PushScreen("main");
        session.PushScreen("settings");
        session.PushScreen("lang");
        session.PushScreen("lang_confirm");

        session.PopScreen().Should().Be("lang");
        session.PopScreen().Should().Be("settings");
        session.PopScreen().Should().Be("main");
        session.PopScreen().Should().BeNull();
        session.CurrentScreen.Should().Be("main");
    }

    [Fact]
    public void PushScreen_DuplicateInStack_TruncatesToExisting()
    {
        var session = new UserSession(1);
        session.PushScreen("main");
        session.PushScreen("profile");
        session.PushScreen("settings");

        session.PushScreen("profile");

        session.CurrentScreen.Should().Be("profile");
        session.NavigationStack.Should().Equal("main");
    }

    [Fact]
    public void PushScreen_SameAsCurrent_NoOp()
    {
        var session = new UserSession(1);
        session.PushScreen("main");
        session.PushScreen("settings");

        session.PushScreen("settings");

        session.CurrentScreen.Should().Be("settings");
        session.NavigationStack.Should().ContainSingle().Which.Should().Be("main");
    }

    [Fact]
    public void PushScreen_ExceedsMaxDepth_DropsOldest()
    {
        var session = new UserSession(1);

        for (int i = 0; i <= UserSession.MAX_NAVIGATION_DEPTH + 5; i++)
            session.PushScreen($"screen_{i}");

        session.NavigationStack.Should().HaveCount(UserSession.MAX_NAVIGATION_DEPTH);
        session.NavigationStack[0].Should().NotBe("screen_0");
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

        session.CurrentScreen.Should().Be("main");
        session.NavigationStack.Should().BeEmpty();
    }
}
