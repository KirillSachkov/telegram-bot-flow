using System.Text.Json;
using FluentAssertions;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.Core.Sessions.Redis;

namespace TelegramBotFlow.Core.Tests.Sessions;

public sealed class RedisSessionStoreTests
{
    // ── ToJson ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToJson_IncludesAllSystemFields()
    {
        var session = new UserSession(42)
        {
            CurrentScreen = "settings:main",
            NavMessageId = 100,
            CurrentMediaType = ScreenMediaType.Photo,
        };
        session.NavigationStack.Add("main");

        JsonElement json = ParseJson(session);

        json.GetProperty("createdAt").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        json.GetProperty("lastActivity").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        json.GetProperty("currentScreen").GetString().Should().Be("settings:main");
        json.GetProperty("navMessageId").GetInt32().Should().Be(100);
        json.GetProperty("navigationStack").GetArrayLength().Should().Be(1);
        json.GetProperty("navigationStack")[0].GetString().Should().Be("main");
    }

    [Fact]
    public void ToJson_UserData_StoredUnderUserDataKey()
    {
        var session = new UserSession(42);
        session.Set("city", "Moscow");
        session.Set("lang", "ru");

        JsonElement json = ParseJson(session);

        json.TryGetProperty("userData", out JsonElement userData).Should().BeTrue();
        userData.GetProperty("city").GetString().Should().Be("Moscow");
        userData.GetProperty("lang").GetString().Should().Be("ru");
    }

    [Fact]
    public void ToJson_EmptyUserData_OmitsUserDataKey()
    {
        var session = new UserSession(42);

        JsonElement json = ParseJson(session);

        json.TryGetProperty("userData", out _).Should().BeFalse();
    }

    [Fact]
    public void ToJson_NullableFields_OmittedWhenNull()
    {
        var session = new UserSession(42);

        JsonElement json = ParseJson(session);

        json.TryGetProperty("currentScreen", out _).Should().BeFalse();
        json.TryGetProperty("navMessageId", out _).Should().BeFalse();
        json.TryGetProperty("navigationStack", out _).Should().BeFalse();
        json.TryGetProperty("pendingInputActionId", out _).Should().BeFalse();
    }

    [Fact]
    public void ToJson_LastActivity_UsesSessionValue_NotUtcNow()
    {
        var session = new UserSession(42);
        var fixedTime = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        session.LastActivity = fixedTime;

        JsonElement json = ParseJson(session);

        json.GetProperty("lastActivity").GetDateTime().Should().Be(fixedTime);
    }

    [Fact]
    public void ToJson_PendingInput_StoredUnderPendingInputActionIdKey()
    {
        var session = new UserSession(42) { PendingInputActionId = "roadmap:set" };

        JsonElement json = ParseJson(session);

        json.TryGetProperty("pendingInputActionId", out JsonElement pending).Should().BeTrue();
        pending.GetString().Should().Be("roadmap:set");
    }

    [Fact]
    public void ToJson_NullPendingInput_OmittedFromJson()
    {
        var session = new UserSession(42) { PendingInputActionId = null };

        JsonElement json = ParseJson(session);

        json.TryGetProperty("pendingInputActionId", out _).Should().BeFalse();
    }

    // ── FromJson ─────────────────────────────────────────────────────────────

    [Fact]
    public void FromJson_RestoresSystemFields()
    {
        var original = new UserSession(42)
        {
            CurrentScreen = "contact:share",
            NavMessageId = 55,
            CurrentMediaType = ScreenMediaType.Video,
        };
        original.NavigationStack.Add("main");
        original.NavigationStack.Add("settings");

        UserSession restored = Roundtrip(original, 42);

        restored.UserId.Should().Be(42);
        restored.CurrentScreen.Should().Be("contact:share");
        restored.NavMessageId.Should().Be(55);
        restored.CurrentMediaType.Should().Be(ScreenMediaType.Video);
        restored.NavigationStack.Should().HaveCount(2);
    }

    [Fact]
    public void FromJson_RestoresCreatedAt()
    {
        var original = new UserSession(42);
        DateTime originalCreatedAt = original.CreatedAt;

        UserSession restored = Roundtrip(original, 42);

        restored.CreatedAt.Should().BeCloseTo(originalCreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void FromJson_RestoresLastActivity()
    {
        var original = new UserSession(42);
        var fixedTime = new DateTime(2025, 3, 10, 8, 0, 0, DateTimeKind.Utc);
        original.LastActivity = fixedTime;

        UserSession restored = Roundtrip(original, 42);

        restored.LastActivity.Should().Be(fixedTime);
    }

    [Fact]
    public void FromJson_RestoresUserData()
    {
        var original = new UserSession(42);
        original.Set("name", "John");
        original.Set("email", "john@test.com");

        UserSession restored = Roundtrip(original, 42);

        restored.GetString("name").Should().Be("John");
        restored.GetString("email").Should().Be("john@test.com");
    }

    [Fact]
    public void FromJson_NullableFields_RemainNull()
    {
        var original = new UserSession(42);

        UserSession restored = Roundtrip(original, 42);

        restored.CurrentScreen.Should().BeNull();
        restored.NavMessageId.Should().BeNull();
        restored.PendingInputActionId.Should().BeNull();
    }

    [Fact]
    public void FromJson_RestoresPendingInput()
    {
        var original = new UserSession(42) { PendingInputActionId = "profile:edit-name" };

        UserSession restored = Roundtrip(original, 42);

        restored.PendingInputActionId.Should().Be("profile:edit-name");
    }

    [Fact]
    public void FromJson_NullPendingInput_RemainsNull()
    {
        var original = new UserSession(42) { PendingInputActionId = null };

        UserSession restored = Roundtrip(original, 42);

        restored.PendingInputActionId.Should().BeNull();
    }

    // ── Roundtrip ─────────────────────────────────────────────────────────────

    [Fact]
    public void Roundtrip_PreservesAllData()
    {
        var original = new UserSession(99)
        {
            CurrentScreen = "settings:lang",
            NavMessageId = 77,
            CurrentMediaType = ScreenMediaType.Photo,
            PendingInputActionId = "settings:set-city",
        };
        original.NavigationStack.Add("main");
        original.Set("age", "25");
        original.Set("name", "Alice");

        UserSession restored = Roundtrip(original, 99);

        restored.UserId.Should().Be(99);
        restored.CurrentScreen.Should().Be("settings:lang");
        restored.NavMessageId.Should().Be(77);
        restored.CurrentMediaType.Should().Be(ScreenMediaType.Photo);
        restored.PendingInputActionId.Should().Be("settings:set-city");
        restored.NavigationStack.Should().ContainSingle().Which.Should().Be("main");
        restored.GetString("age").Should().Be("25");
        restored.GetString("name").Should().Be("Alice");
        restored.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public void Roundtrip_RemovedKeys_DoNotLeak()
    {
        var session = new UserSession(42);
        session.Set("temp_code", "1234");
        session.Set("city", "Moscow");

        string json1 = RedisSessionStore.ToJson(session);
        UserSession afterFirst = RedisSessionStore.FromJson(42, json1);
        afterFirst.GetAll().Should().ContainKey("temp_code");
        afterFirst.GetAll().Should().ContainKey("city");

        session.Remove("temp_code");

        UserSession restored = Roundtrip(session, 42);

        restored.GetString("temp_code").Should().BeNull();
        restored.Has("temp_code").Should().BeFalse();
        restored.GetString("city").Should().Be("Moscow");
        restored.GetAll().Should().HaveCount(1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static UserSession Roundtrip(UserSession session, long userId)
    {
        string json = RedisSessionStore.ToJson(session);
        return RedisSessionStore.FromJson(userId, json);
    }

    private static JsonElement ParseJson(UserSession session)
    {
        string json = RedisSessionStore.ToJson(session);
        return JsonSerializer.Deserialize<JsonElement>(json);
    }
}
