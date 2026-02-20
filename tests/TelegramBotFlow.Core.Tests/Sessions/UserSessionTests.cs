using FluentAssertions;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Sessions;

public sealed class UserSessionTests
{
    [Fact]
    public void Set_And_GetString_WorkCorrectly()
    {
        var session = new UserSession(1);
        session.Set("name", "Test");

        _ = session.GetString("name").Should().Be("Test");
    }

    [Fact]
    public void GetString_NonExistentKey_ReturnsNull()
    {
        var session = new UserSession(1);

        _ = session.GetString("missing").Should().BeNull();
    }

    [Fact]
    public void GetInt_ParsesStringToInt()
    {
        var session = new UserSession(1);
        session.Set("age", "25");

        _ = session.GetInt("age").Should().Be(25);
        _ = session.GetInt("missing").Should().BeNull();
    }

    [Fact]
    public void GetBool_ParsesTrueString()
    {
        var session = new UserSession(1);
        session.Set("active", "true");
        session.Set("inactive", "false");

        _ = session.GetBool("active").Should().BeTrue();
        _ = session.GetBool("inactive").Should().BeFalse();
        _ = session.GetBool("missing").Should().BeFalse();
    }

    [Fact]
    public void Has_ReturnsTrueForExistingKey()
    {
        var session = new UserSession(1);
        session.Set("key", "value");

        _ = session.Has("key").Should().BeTrue();
        _ = session.Has("other").Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAllDataAndResetsNavigation()
    {
        var session = new UserSession(1);
        session.Set("key", "value");
        session.CurrentScreen = "settings:main";
        session.NavMessageId = 100;
        session.NavigationStack.Add("main");

        session.Clear();

        _ = session.Has("key").Should().BeFalse();
        _ = session.CurrentScreen.Should().BeNull();
        _ = session.NavMessageId.Should().BeNull();
        _ = session.NavigationStack.Should().BeEmpty();
        _ = session.CurrentMediaType.Should().Be(ScreenMediaType.None);
    }

    [Fact]
    public void CurrentScreen_CanBeSetAndRead()
    {
        var session = new UserSession(1);

        _ = session.CurrentScreen.Should().BeNull();

        session.CurrentScreen = "settings:main";

        _ = session.CurrentScreen.Should().Be("settings:main");
    }

    [Fact]
    public void PushScreen_AddsCurrentScreenToStack()
    {
        var session = new UserSession(1);

        session.PushScreen("main");
        _ = session.CurrentScreen.Should().Be("main");
        _ = session.NavigationStack.Should().BeEmpty();

        session.PushScreen("settings");
        _ = session.CurrentScreen.Should().Be("settings");
        _ = session.NavigationStack.Should().ContainSingle().Which.Should().Be("main");

        session.PushScreen("lang");
        _ = session.CurrentScreen.Should().Be("lang");
        _ = session.NavigationStack.Should().HaveCount(2);
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

        popped = session.PopScreen();
        _ = popped.Should().Be("main");
        _ = session.CurrentScreen.Should().Be("main");

        popped = session.PopScreen();
        _ = popped.Should().BeNull();
    }
}