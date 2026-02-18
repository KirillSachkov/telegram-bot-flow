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

        session.GetString("name").Should().Be("Test");
    }

    [Fact]
    public void GetString_NonExistentKey_ReturnsNull()
    {
        var session = new UserSession(1);

        session.GetString("missing").Should().BeNull();
    }

    [Fact]
    public void GetInt_ParsesStringToInt()
    {
        var session = new UserSession(1);
        session.Set("age", "25");

        session.GetInt("age").Should().Be(25);
        session.GetInt("missing").Should().BeNull();
    }

    [Fact]
    public void GetBool_ParsesTrueString()
    {
        var session = new UserSession(1);
        session.Set("active", "true");
        session.Set("inactive", "false");

        session.GetBool("active").Should().BeTrue();
        session.GetBool("inactive").Should().BeFalse();
        session.GetBool("missing").Should().BeFalse();
    }

    [Fact]
    public void Has_ReturnsTrueForExistingKey()
    {
        var session = new UserSession(1);
        session.Set("key", "value");

        session.Has("key").Should().BeTrue();
        session.Has("other").Should().BeFalse();
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

        session.Has("key").Should().BeFalse();
        session.CurrentScreen.Should().BeNull();
        session.NavMessageId.Should().BeNull();
        session.NavigationStack.Should().BeEmpty();
        session.CurrentMediaType.Should().Be(ScreenMediaType.None);
    }

    [Fact]
    public void CurrentScreen_CanBeSetAndRead()
    {
        var session = new UserSession(1);

        session.CurrentScreen.Should().BeNull();

        session.CurrentScreen = "settings:main";

        session.CurrentScreen.Should().Be("settings:main");
    }

    [Fact]
    public void PushScreen_AddsCurrentScreenToStack()
    {
        var session = new UserSession(1);

        session.PushScreen("main");
        session.CurrentScreen.Should().Be("main");
        session.NavigationStack.Should().BeEmpty();

        session.PushScreen("settings");
        session.CurrentScreen.Should().Be("settings");
        session.NavigationStack.Should().ContainSingle().Which.Should().Be("main");

        session.PushScreen("lang");
        session.CurrentScreen.Should().Be("lang");
        session.NavigationStack.Should().HaveCount(2);
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

        popped = session.PopScreen();
        popped.Should().Be("main");
        session.CurrentScreen.Should().Be("main");

        popped = session.PopScreen();
        popped.Should().BeNull();
    }
}