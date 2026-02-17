using FluentAssertions;
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
    public void Clear_RemovesAllDataAndResetsFlowAndScreen()
    {
        var session = new UserSession(1);
        session.Set("key", "value");
        session.CurrentFlowId = "reg";
        session.CurrentStepId = "step1";
        session.CurrentScreen = "settings:main";

        session.Clear();

        session.Has("key").Should().BeFalse();
        session.CurrentFlowId.Should().BeNull();
        session.CurrentStepId.Should().BeNull();
        session.CurrentScreen.Should().BeNull();
        session.IsInFlow.Should().BeFalse();
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
    public void IsInFlow_ReflectsCurrentFlowId()
    {
        var session = new UserSession(1);

        session.IsInFlow.Should().BeFalse();

        session.CurrentFlowId = "flow1";

        session.IsInFlow.Should().BeTrue();
    }
}
