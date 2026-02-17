using FluentAssertions;
using StackExchange.Redis;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.IntegrationTests.Infrastructure;

namespace TelegramBotFlow.IntegrationTests.Sessions;

public sealed class RedisSessionStoreIntegrationTests : RedisSessionTestsBase
{
    public RedisSessionStoreIntegrationTests(RedisFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task GetOrCreate_NewUser_CreatesSessionInRedis()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var session = await store.GetOrCreateAsync(100);

        // Assert
        session.UserId.Should().Be(100);

        var exists = await Db.KeyExistsAsync("bot:session:100");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrCreate_ExistingUser_ReturnsSameSession()
    {
        // Arrange
        var store = CreateStore();
        var original = await store.GetOrCreateAsync(200);
        original.Set("name", "Alice");
        original.CurrentFlowId = "reg";
        await store.SaveAsync(original);

        // Act
        var restored = await store.GetOrCreateAsync(200);

        // Assert
        restored.UserId.Should().Be(200);
        restored.GetString("name").Should().Be("Alice");
        restored.CurrentFlowId.Should().Be("reg");
    }

    [Fact]
    public async Task Save_WritesAllSystemFields()
    {
        // Arrange
        var store = CreateStore();
        var session = await store.GetOrCreateAsync(300);
        session.CurrentFlowId = "onboarding";
        session.CurrentStepId = "step1";
        session.CurrentScreen = "settings:main";

        // Act
        await store.SaveAsync(session);

        // Assert
        var entries = await Db.HashGetAllAsync("bot:session:300");
        var dict = entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());

        dict.Should().ContainKey(RedisSessionStore.FieldCreatedAt);
        dict.Should().ContainKey(RedisSessionStore.FieldLastActivity);
        dict[RedisSessionStore.FieldFlowId].Should().Be("onboarding");
        dict[RedisSessionStore.FieldStepId].Should().Be("step1");
        dict[RedisSessionStore.FieldScreen].Should().Be("settings:main");
    }

    [Fact]
    public async Task Save_WritesUserDataAsJson()
    {
        // Arrange
        var store = CreateStore();
        var session = await store.GetOrCreateAsync(400);
        session.Set("city", "Moscow");
        session.Set("lang", "ru");

        // Act
        await store.SaveAsync(session);

        // Assert
        var userDataRaw = await Db.HashGetAsync("bot:session:400", RedisSessionStore.FieldUserData);
        userDataRaw.IsNullOrEmpty.Should().BeFalse();
        userDataRaw.ToString().Should().Contain("Moscow").And.Contain("ru");
    }

    [Fact]
    public async Task Roundtrip_PreservesAllData()
    {
        // Arrange
        var store = CreateStore();
        var original = await store.GetOrCreateAsync(500);
        original.CurrentFlowId = "registration";
        original.CurrentStepId = "email";
        original.CurrentScreen = "contact:share";
        original.Set("age", "30");
        original.Set("city", "Berlin");
        await store.SaveAsync(original);

        // Act
        var restored = await store.GetOrCreateAsync(500);

        // Assert
        restored.UserId.Should().Be(500);
        restored.CurrentFlowId.Should().Be("registration");
        restored.CurrentStepId.Should().Be("email");
        restored.CurrentScreen.Should().Be("contact:share");
        restored.GetString("age").Should().Be("30");
        restored.GetString("city").Should().Be("Berlin");
        restored.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public async Task Roundtrip_PreservesCreatedAt()
    {
        // Arrange
        var store = CreateStore();
        var original = await store.GetOrCreateAsync(600);
        var originalCreatedAt = original.CreatedAt;
        await store.SaveAsync(original);

        // Act
        var restored = await store.GetOrCreateAsync(600);

        // Assert — CreatedAt не должен сбрасываться при десериализации
        restored.CreatedAt.Should().BeCloseTo(originalCreatedAt, TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public async Task Save_UpdatesLastActivity()
    {
        // Arrange
        var store = CreateStore();
        var session = await store.GetOrCreateAsync(700);
        var firstActivity = session.LastActivity;

        await Task.Delay(50);

        // Act
        await store.SaveAsync(session);

        // Assert
        session.LastActivity.Should().BeAfter(firstActivity);

        var restored = await store.GetOrCreateAsync(700);
        restored.LastActivity.Should().BeAfter(firstActivity);
    }

    [Fact]
    public async Task RemovedKeys_DoNotPersist()
    {
        // Arrange
        var store = CreateStore();
        var session = await store.GetOrCreateAsync(800);
        session.Set("temp_code", "1234");
        session.Set("city", "Moscow");
        await store.SaveAsync(session);

        // Act — удаляем ключ и сохраняем снова
        session.Remove("temp_code");
        await store.SaveAsync(session);

        // Assert
        var restored = await store.GetOrCreateAsync(800);
        restored.Has("temp_code").Should().BeFalse();
        restored.GetString("temp_code").Should().BeNull();
        restored.GetString("city").Should().Be("Moscow");
        restored.GetAll().Should().HaveCount(1);
    }

    [Fact]
    public async Task ClearedSession_PersistsCorrectly()
    {
        // Arrange
        var store = CreateStore();
        var session = await store.GetOrCreateAsync(900);
        session.CurrentFlowId = "reg";
        session.CurrentStepId = "name";
        session.CurrentScreen = "main";
        session.Set("data", "value");
        await store.SaveAsync(session);

        // Act
        session.Clear();
        await store.SaveAsync(session);

        // Assert
        var restored = await store.GetOrCreateAsync(900);
        restored.CurrentFlowId.Should().BeNull();
        restored.CurrentStepId.Should().BeNull();
        restored.CurrentScreen.Should().BeNull();
        restored.IsInFlow.Should().BeFalse();
        restored.GetAll().Should().BeEmpty();
    }

    [Fact]
    public async Task SessionTtl_SetsExpireOnKey()
    {
        // Arrange — TTL = 5 минут
        var store = CreateStore(sessionTtlMinutes: 5);
        var session = await store.GetOrCreateAsync(1000);

        // Act
        await store.SaveAsync(session);

        // Assert
        var ttl = await Db.KeyTimeToLiveAsync("bot:session:1000");
        ttl.Should().NotBeNull();
        ttl!.Value.TotalMinutes.Should().BeGreaterThan(4).And.BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task NoSessionTtl_KeyHasNoExpire()
    {
        // Arrange — без TTL
        var store = CreateStore(sessionTtlMinutes: null);
        var session = await store.GetOrCreateAsync(1100);

        // Act
        await store.SaveAsync(session);

        // Assert
        var ttl = await Db.KeyTimeToLiveAsync("bot:session:1100");
        ttl.Should().BeNull();
    }

    [Fact]
    public async Task MultipleSaves_OverwriteCleanly()
    {
        // Arrange
        var store = CreateStore();
        var session = await store.GetOrCreateAsync(1200);

        // Первое сохранение
        session.Set("step", "1");
        session.CurrentFlowId = "flow_v1";
        await store.SaveAsync(session);

        // Второе сохранение — изменённые данные
        session.Set("step", "2");
        session.CurrentFlowId = "flow_v2";
        await store.SaveAsync(session);

        // Act
        var restored = await store.GetOrCreateAsync(1200);

        // Assert
        restored.GetString("step").Should().Be("2");
        restored.CurrentFlowId.Should().Be("flow_v2");
    }

    [Fact]
    public async Task DifferentUsers_IndependentSessions()
    {
        // Arrange
        var store = CreateStore();

        var sessionA = await store.GetOrCreateAsync(1300);
        sessionA.Set("name", "Alice");
        await store.SaveAsync(sessionA);

        var sessionB = await store.GetOrCreateAsync(1400);
        sessionB.Set("name", "Bob");
        await store.SaveAsync(sessionB);

        // Act
        var restoredA = await store.GetOrCreateAsync(1300);
        var restoredB = await store.GetOrCreateAsync(1400);

        // Assert
        restoredA.GetString("name").Should().Be("Alice");
        restoredB.GetString("name").Should().Be("Bob");
    }

    [Fact]
    public async Task FixedFieldCount_InRedisHash()
    {
        // Arrange
        var store = CreateStore();
        var session = await store.GetOrCreateAsync(1500);
        session.Set("key1", "val1");
        session.Set("key2", "val2");
        await store.SaveAsync(session);

        // Act
        var entries = await Db.HashGetAllAsync("bot:session:1500");

        // Assert — всегда 6 полей (системные + user_data), независимо от количества данных
        entries.Should().HaveCount(6);
    }
}
