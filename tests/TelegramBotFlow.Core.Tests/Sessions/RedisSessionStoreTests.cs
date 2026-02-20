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
            CurrentScreen = "settings:main", NavMessageId = 100, CurrentMediaType = ScreenMediaType.Photo,
        };
        session.NavigationStack.Add("main");

        JsonElement json = ParseJson(session);

        _ = json.GetProperty("createdAt").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _ = json.GetProperty("lastActivity").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _ = json.GetProperty("currentScreen").GetString().Should().Be("settings:main");
        _ = json.GetProperty("navMessageId").GetInt32().Should().Be(100);
        _ = json.GetProperty("navigationStack").GetArrayLength().Should().Be(1);
        _ = json.GetProperty("navigationStack")[0].GetString().Should().Be("main");
    }

    [Fact]
    public void ToJson_UserData_StoredUnderUserDataKey()
    {
        var session = new UserSession(42);
        session.Set("city", "Moscow");
        session.Set("lang", "ru");

        JsonElement json = ParseJson(session);

        _ = json.TryGetProperty("userData", out JsonElement userData).Should().BeTrue();
        _ = userData.GetProperty("city").GetString().Should().Be("Moscow");
        _ = userData.GetProperty("lang").GetString().Should().Be("ru");
    }

    [Fact]
    public void ToJson_EmptyUserData_OmitsUserDataKey()
    {
        var session = new UserSession(42);

        JsonElement json = ParseJson(session);

        _ = json.TryGetProperty("userData", out _).Should().BeFalse();
    }

    [Fact]
    public void ToJson_NullableFields_OmittedWhenNull()
    {
        var session = new UserSession(42);

        JsonElement json = ParseJson(session);

        _ = json.TryGetProperty("currentScreen", out _).Should().BeFalse();
        _ = json.TryGetProperty("navMessageId", out _).Should().BeFalse();
        _ = json.TryGetProperty("navigationStack", out _).Should().BeFalse();
        _ = json.TryGetProperty("pendingInputActionId", out _).Should().BeFalse();
    }

    [Fact]
    public void ToJson_LastActivity_UsesSessionValue_NotUtcNow()
    {
        var session = new UserSession(42);
        var fixedTime = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        session.LastActivity = fixedTime;

        JsonElement json = ParseJson(session);

        _ = json.GetProperty("lastActivity").GetDateTime().Should().Be(fixedTime);
    }

    [Fact]
    public void ToJson_PendingInput_StoredUnderPendingInputActionIdKey()
    {
        var session = new UserSession(42) { PendingInputActionId = "roadmap:set" };

        JsonElement json = ParseJson(session);

        _ = json.TryGetProperty("pendingInputActionId", out JsonElement pending).Should().BeTrue();
        _ = pending.GetString().Should().Be("roadmap:set");
    }

    [Fact]
    public void ToJson_NullPendingInput_OmittedFromJson()
    {
        var session = new UserSession(42) { PendingInputActionId = null };

        JsonElement json = ParseJson(session);

        _ = json.TryGetProperty("pendingInputActionId", out _).Should().BeFalse();
    }

    // ── FromJson ─────────────────────────────────────────────────────────────

    [Fact]
    public void FromJson_RestoresSystemFields()
    {
        var original = new UserSession(42)
        {
            CurrentScreen = "contact:share", NavMessageId = 55, CurrentMediaType = ScreenMediaType.Video,
        };
        original.NavigationStack.Add("main");
        original.NavigationStack.Add("settings");

        UserSession restored = Roundtrip(original, 42);

        _ = restored.UserId.Should().Be(42);
        _ = restored.CurrentScreen.Should().Be("contact:share");
        _ = restored.NavMessageId.Should().Be(55);
        _ = restored.CurrentMediaType.Should().Be(ScreenMediaType.Video);
        _ = restored.NavigationStack.Should().HaveCount(2);
    }

    [Fact]
    public void FromJson_RestoresCreatedAt()
    {
        var original = new UserSession(42);
        DateTime originalCreatedAt = original.CreatedAt;

        UserSession restored = Roundtrip(original, 42);

        _ = restored.CreatedAt.Should().BeCloseTo(originalCreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void FromJson_RestoresLastActivity()
    {
        var original = new UserSession(42);
        var fixedTime = new DateTime(2025, 3, 10, 8, 0, 0, DateTimeKind.Utc);
        original.LastActivity = fixedTime;

        UserSession restored = Roundtrip(original, 42);

        _ = restored.LastActivity.Should().Be(fixedTime);
    }

    [Fact]
    public void FromJson_RestoresUserData()
    {
        var original = new UserSession(42);
        original.Set("name", "John");
        original.Set("email", "john@test.com");

        UserSession restored = Roundtrip(original, 42);

        _ = restored.GetString("name").Should().Be("John");
        _ = restored.GetString("email").Should().Be("john@test.com");
    }

    [Fact]
    public void FromJson_NullableFields_RemainNull()
    {
        var original = new UserSession(42);

        UserSession restored = Roundtrip(original, 42);

        _ = restored.CurrentScreen.Should().BeNull();
        _ = restored.NavMessageId.Should().BeNull();
        _ = restored.PendingInputActionId.Should().BeNull();
    }

    [Fact]
    public void FromJson_RestoresPendingInput()
    {
        var original = new UserSession(42) { PendingInputActionId = "profile:edit-name" };

        UserSession restored = Roundtrip(original, 42);

        _ = restored.PendingInputActionId.Should().Be("profile:edit-name");
    }

    [Fact]
    public void FromJson_NullPendingInput_RemainsNull()
    {
        var original = new UserSession(42) { PendingInputActionId = null };

        UserSession restored = Roundtrip(original, 42);

        _ = restored.PendingInputActionId.Should().BeNull();
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

        _ = restored.UserId.Should().Be(99);
        _ = restored.CurrentScreen.Should().Be("settings:lang");
        _ = restored.NavMessageId.Should().Be(77);
        _ = restored.CurrentMediaType.Should().Be(ScreenMediaType.Photo);
        _ = restored.PendingInputActionId.Should().Be("settings:set-city");
        _ = restored.NavigationStack.Should().ContainSingle().Which.Should().Be("main");
        _ = restored.GetString("age").Should().Be("25");
        _ = restored.GetString("name").Should().Be("Alice");
        _ = restored.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public void Roundtrip_RemovedKeys_DoNotLeak()
    {
        var session = new UserSession(42);
        session.Set("temp_code", "1234");
        session.Set("city", "Moscow");

        string json1 = RedisSessionStore.ToJson(session);
        UserSession afterFirst = RedisSessionStore.FromJson(42, json1);
        _ = afterFirst.GetAll().Should().ContainKey("temp_code");
        _ = afterFirst.GetAll().Should().ContainKey("city");

        session.Remove("temp_code");

        UserSession restored = Roundtrip(session, 42);

        _ = restored.GetString("temp_code").Should().BeNull();
        _ = restored.Has("temp_code").Should().BeFalse();
        _ = restored.GetString("city").Should().Be("Moscow");
        _ = restored.GetAll().Should().HaveCount(1);
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
