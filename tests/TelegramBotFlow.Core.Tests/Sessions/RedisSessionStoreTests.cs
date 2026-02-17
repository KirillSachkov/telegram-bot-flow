using System.Text.Json;
using FluentAssertions;
using StackExchange.Redis;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Sessions;

public sealed class RedisSessionStoreTests
{
    [Fact]
    public void Serialize_IncludesAllSystemFields()
    {
        var session = new UserSession(42)
        {
            CurrentFlowId = "reg",
            CurrentStepId = "name",
            CurrentScreen = "settings:main"
        };

        var entries = RedisSessionStore.Serialize(session);
        var dict = ToDictionary(entries);

        dict.Should().ContainKey(RedisSessionStore.FieldCreatedAt);
        dict.Should().ContainKey(RedisSessionStore.FieldLastActivity);
        dict[RedisSessionStore.FieldFlowId].Should().Be("reg");
        dict[RedisSessionStore.FieldStepId].Should().Be("name");
        dict[RedisSessionStore.FieldScreen].Should().Be("settings:main");
    }

    [Fact]
    public void Serialize_UserData_StoredAsJson()
    {
        var session = new UserSession(42);
        session.Set("city", "Moscow");
        session.Set("lang", "ru");

        var entries = RedisSessionStore.Serialize(session);
        var dict = ToDictionary(entries);

        dict.Should().ContainKey(RedisSessionStore.FieldUserData);
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(dict[RedisSessionStore.FieldUserData]);
        data.Should().ContainKey("city").WhoseValue.Should().Be("Moscow");
        data.Should().ContainKey("lang").WhoseValue.Should().Be("ru");
    }

    [Fact]
    public void Serialize_EmptyUserData_StoredAsEmptyString()
    {
        var session = new UserSession(42);

        var entries = RedisSessionStore.Serialize(session);
        var dict = ToDictionary(entries);

        dict[RedisSessionStore.FieldUserData].Should().BeEmpty();
    }

    [Fact]
    public void Serialize_NullableFields_StoredAsEmpty()
    {
        var session = new UserSession(42);

        var entries = RedisSessionStore.Serialize(session);
        var dict = ToDictionary(entries);

        dict[RedisSessionStore.FieldFlowId].Should().BeEmpty();
        dict[RedisSessionStore.FieldStepId].Should().BeEmpty();
        dict[RedisSessionStore.FieldScreen].Should().BeEmpty();
    }

    [Fact]
    public void Serialize_LastActivity_UsesSessionValue_NotUtcNow()
    {
        var session = new UserSession(42);
        var fixedTime = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        session.LastActivity = fixedTime;

        var entries = RedisSessionStore.Serialize(session);
        var dict = ToDictionary(entries);

        dict[RedisSessionStore.FieldLastActivity].Should().Be(fixedTime.ToString("O"));
    }

    [Fact]
    public void Deserialize_RestoresSystemFields()
    {
        var original = new UserSession(42)
        {
            CurrentFlowId = "reg",
            CurrentStepId = "email",
            CurrentScreen = "contact:share"
        };

        var entries = RedisSessionStore.Serialize(original);
        var restored = RedisSessionStore.Deserialize(42, entries);

        restored.UserId.Should().Be(42);
        restored.CurrentFlowId.Should().Be("reg");
        restored.CurrentStepId.Should().Be("email");
        restored.CurrentScreen.Should().Be("contact:share");
        restored.IsInFlow.Should().BeTrue();
    }

    [Fact]
    public void Deserialize_RestoresCreatedAt()
    {
        var original = new UserSession(42);
        var originalCreatedAt = original.CreatedAt;

        var entries = RedisSessionStore.Serialize(original);

        // Simulate delay — new UserSession(42) would set a different CreatedAt
        var restored = RedisSessionStore.Deserialize(42, entries);

        restored.CreatedAt.Should().BeCloseTo(originalCreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void Deserialize_RestoresLastActivity()
    {
        var original = new UserSession(42);
        var fixedTime = new DateTime(2025, 3, 10, 8, 0, 0, DateTimeKind.Utc);
        original.LastActivity = fixedTime;

        var entries = RedisSessionStore.Serialize(original);
        var restored = RedisSessionStore.Deserialize(42, entries);

        restored.LastActivity.Should().Be(fixedTime);
    }

    [Fact]
    public void Deserialize_RestoresUserData()
    {
        var original = new UserSession(42);
        original.Set("flow:reg:name", "John");
        original.Set("flow:reg:email", "john@test.com");

        var entries = RedisSessionStore.Serialize(original);
        var restored = RedisSessionStore.Deserialize(42, entries);

        restored.GetString("flow:reg:name").Should().Be("John");
        restored.GetString("flow:reg:email").Should().Be("john@test.com");
    }

    [Fact]
    public void Deserialize_EmptyFields_BecomeNull()
    {
        var original = new UserSession(42);

        var entries = RedisSessionStore.Serialize(original);
        var restored = RedisSessionStore.Deserialize(42, entries);

        restored.CurrentFlowId.Should().BeNull();
        restored.CurrentStepId.Should().BeNull();
        restored.CurrentScreen.Should().BeNull();
        restored.IsInFlow.Should().BeFalse();
    }

    [Fact]
    public void Roundtrip_PreservesAllData()
    {
        var original = new UserSession(99)
        {
            CurrentFlowId = "onboarding",
            CurrentStepId = "step2",
            CurrentScreen = "settings:lang"
        };
        original.Set("age", "25");
        original.Set("name", "Alice");

        var entries = RedisSessionStore.Serialize(original);
        var restored = RedisSessionStore.Deserialize(99, entries);

        restored.UserId.Should().Be(99);
        restored.CurrentFlowId.Should().Be("onboarding");
        restored.CurrentStepId.Should().Be("step2");
        restored.CurrentScreen.Should().Be("settings:lang");
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

        // First save — both keys present
        var entries1 = RedisSessionStore.Serialize(session);
        var dict1 = ToDictionary(entries1);
        var data1 = JsonSerializer.Deserialize<Dictionary<string, string>>(dict1[RedisSessionStore.FieldUserData]);
        data1.Should().ContainKey("temp_code");
        data1.Should().ContainKey("city");

        // Simulate remove
        session.Remove("temp_code");

        // Second save — temp_code is gone
        var entries2 = RedisSessionStore.Serialize(session);
        var restored = RedisSessionStore.Deserialize(42, entries2);

        restored.GetString("temp_code").Should().BeNull();
        restored.Has("temp_code").Should().BeFalse();
        restored.GetString("city").Should().Be("Moscow");
        restored.GetAll().Should().HaveCount(1);
    }

    [Fact]
    public void Serialize_FixedFieldCount()
    {
        var session = new UserSession(42);

        var entries = RedisSessionStore.Serialize(session);

        // 6 system fields + user_data
        entries.Should().HaveCount(6);
    }

    private static Dictionary<string, string> ToDictionary(HashEntry[] entries) =>
        entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
}
