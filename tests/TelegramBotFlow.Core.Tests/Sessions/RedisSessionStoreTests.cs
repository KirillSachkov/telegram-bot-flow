using System.Text.Json;
using FluentAssertions;
using StackExchange.Redis;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.Core.Sessions.Redis;

namespace TelegramBotFlow.Core.Tests.Sessions;

public sealed class RedisSessionStoreTests
{
    [Fact]
    public void Serialize_IncludesAllSystemFields()
    {
        var session = new UserSession(42)
        {
            CurrentScreen = "settings:main",
            NavMessageId = 100,
            CurrentMediaType = ScreenMediaType.Photo,
        };
        session.NavigationStack.Add("main");

        HashEntry[] entries = RedisSessionStore.Serialize(session);
        Dictionary<string, string> dict = ToDictionary(entries);

        dict.Should().ContainKey(RedisSessionStore.FieldCreatedAt);
        dict.Should().ContainKey(RedisSessionStore.FieldLastActivity);
        dict[RedisSessionStore.FieldScreen].Should().Be("settings:main");
        dict[RedisSessionStore.FieldNavMessageId].Should().Be("100");
        dict[RedisSessionStore.FieldMediaType].Should().Be("Photo");
        dict[RedisSessionStore.FieldNavigationStack].Should().Contain("main");
    }

    [Fact]
    public void Serialize_UserData_StoredAsJson()
    {
        var session = new UserSession(42);
        session.Set("city", "Moscow");
        session.Set("lang", "ru");

        HashEntry[] entries = RedisSessionStore.Serialize(session);
        Dictionary<string, string> dict = ToDictionary(entries);

        dict.Should().ContainKey(RedisSessionStore.FieldUserData);
        Dictionary<string, string>? data = JsonSerializer.Deserialize<Dictionary<string, string>>(dict[RedisSessionStore.FieldUserData]);
        data.Should().ContainKey("city").WhoseValue.Should().Be("Moscow");
        data.Should().ContainKey("lang").WhoseValue.Should().Be("ru");
    }

    [Fact]
    public void Serialize_EmptyUserData_StoredAsEmptyString()
    {
        var session = new UserSession(42);

        HashEntry[] entries = RedisSessionStore.Serialize(session);
        Dictionary<string, string> dict = ToDictionary(entries);

        dict[RedisSessionStore.FieldUserData].Should().BeEmpty();
    }

    [Fact]
    public void Serialize_NullableFields_StoredAsEmpty()
    {
        var session = new UserSession(42);

        HashEntry[] entries = RedisSessionStore.Serialize(session);
        Dictionary<string, string> dict = ToDictionary(entries);

        dict[RedisSessionStore.FieldScreen].Should().BeEmpty();
        dict[RedisSessionStore.FieldNavMessageId].Should().BeEmpty();
    }

    [Fact]
    public void Serialize_LastActivity_UsesSessionValue_NotUtcNow()
    {
        var session = new UserSession(42);
        var fixedTime = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        session.LastActivity = fixedTime;

        HashEntry[] entries = RedisSessionStore.Serialize(session);
        Dictionary<string, string> dict = ToDictionary(entries);

        dict[RedisSessionStore.FieldLastActivity].Should().Be(fixedTime.ToString("O"));
    }

    [Fact]
    public void Deserialize_RestoresSystemFields()
    {
        var original = new UserSession(42)
        {
            CurrentScreen = "contact:share",
            NavMessageId = 55,
            CurrentMediaType = ScreenMediaType.Video
        };
        original.NavigationStack.Add("main");
        original.NavigationStack.Add("settings");

        HashEntry[] entries = RedisSessionStore.Serialize(original);
        UserSession restored = RedisSessionStore.Deserialize(42, entries);

        restored.UserId.Should().Be(42);
        restored.CurrentScreen.Should().Be("contact:share");
        restored.NavMessageId.Should().Be(55);
        restored.CurrentMediaType.Should().Be(ScreenMediaType.Video);
        restored.NavigationStack.Should().HaveCount(2);
    }

    [Fact]
    public void Deserialize_RestoresCreatedAt()
    {
        var original = new UserSession(42);
        DateTime originalCreatedAt = original.CreatedAt;

        HashEntry[] entries = RedisSessionStore.Serialize(original);
        UserSession restored = RedisSessionStore.Deserialize(42, entries);

        restored.CreatedAt.Should().BeCloseTo(originalCreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void Deserialize_RestoresLastActivity()
    {
        var original = new UserSession(42);
        var fixedTime = new DateTime(2025, 3, 10, 8, 0, 0, DateTimeKind.Utc);
        original.LastActivity = fixedTime;

        HashEntry[] entries = RedisSessionStore.Serialize(original);
        UserSession restored = RedisSessionStore.Deserialize(42, entries);

        restored.LastActivity.Should().Be(fixedTime);
    }

    [Fact]
    public void Deserialize_RestoresUserData()
    {
        var original = new UserSession(42);
        original.Set("name", "John");
        original.Set("email", "john@test.com");

        HashEntry[] entries = RedisSessionStore.Serialize(original);
        UserSession restored = RedisSessionStore.Deserialize(42, entries);

        restored.GetString("name").Should().Be("John");
        restored.GetString("email").Should().Be("john@test.com");
    }

    [Fact]
    public void Deserialize_EmptyFields_BecomeNull()
    {
        var original = new UserSession(42);

        HashEntry[] entries = RedisSessionStore.Serialize(original);
        UserSession restored = RedisSessionStore.Deserialize(42, entries);

        restored.CurrentScreen.Should().BeNull();
        restored.NavMessageId.Should().BeNull();
    }

    [Fact]
    public void Roundtrip_PreservesAllData()
    {
        var original = new UserSession(99)
        {
            CurrentScreen = "settings:lang",
            NavMessageId = 77,
            CurrentMediaType = ScreenMediaType.Photo,
        };
        original.NavigationStack.Add("main");
        original.Set("age", "25");
        original.Set("name", "Alice");

        HashEntry[] entries = RedisSessionStore.Serialize(original);
        UserSession restored = RedisSessionStore.Deserialize(99, entries);

        restored.UserId.Should().Be(99);
        restored.CurrentScreen.Should().Be("settings:lang");
        restored.NavMessageId.Should().Be(77);
        restored.CurrentMediaType.Should().Be(ScreenMediaType.Photo);
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

        HashEntry[] entries1 = RedisSessionStore.Serialize(session);
        Dictionary<string, string> dict1 = ToDictionary(entries1);
        Dictionary<string, string>? data1 = JsonSerializer.Deserialize<Dictionary<string, string>>(dict1[RedisSessionStore.FieldUserData]);
        data1.Should().ContainKey("temp_code");
        data1.Should().ContainKey("city");

        session.Remove("temp_code");

        HashEntry[] entries2 = RedisSessionStore.Serialize(session);
        UserSession restored = RedisSessionStore.Deserialize(42, entries2);

        restored.GetString("temp_code").Should().BeNull();
        restored.Has("temp_code").Should().BeFalse();
        restored.GetString("city").Should().Be("Moscow");
        restored.GetAll().Should().HaveCount(1);
    }

    [Fact]
    public void Serialize_FixedFieldCount()
    {
        var session = new UserSession(42);

        HashEntry[] entries = RedisSessionStore.Serialize(session);

        entries.Should().HaveCount(7);
    }

    private static Dictionary<string, string> ToDictionary(HashEntry[] entries) =>
        entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
}
