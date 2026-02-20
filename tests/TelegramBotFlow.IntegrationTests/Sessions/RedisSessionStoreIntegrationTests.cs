using System.Text.Json;
using FluentAssertions;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.Core.Sessions.Redis;
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
        RedisSessionStore store = CreateStore();

        UserSession session = await store.GetOrCreateAsync(100);

        _ = session.UserId.Should().Be(100);

        bool exists = await Db.KeyExistsAsync("bot:session:100");
        _ = exists.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrCreate_ExistingUser_ReturnsSameSession()
    {
        RedisSessionStore store = CreateStore();
        UserSession original = await store.GetOrCreateAsync(200);
        original.Set("name", "Alice");
        original.CurrentScreen = "main";
        await store.SaveAsync(original);

        UserSession restored = await store.GetOrCreateAsync(200);

        _ = restored.UserId.Should().Be(200);
        _ = restored.GetString("name").Should().Be("Alice");
        _ = restored.CurrentScreen.Should().Be("main");
    }

    [Fact]
    public async Task Save_WritesAllSystemFields()
    {
        RedisSessionStore store = CreateStore();
        UserSession session = await store.GetOrCreateAsync(300);
        session.CurrentScreen = "settings:main";
        session.NavMessageId = 42;
        session.NavigationStack.Add("main");

        await store.SaveAsync(session);

        string json = (await Db.StringGetAsync("bot:session:300"))!;
        _ = json.Should().NotBeNullOrEmpty();

        JsonElement element = JsonSerializer.Deserialize<JsonElement>(json);
        _ = element.TryGetProperty("createdAt", out _).Should().BeTrue();
        _ = element.TryGetProperty("lastActivity", out _).Should().BeTrue();
        _ = element.GetProperty("currentScreen").GetString().Should().Be("settings:main");
        _ = element.GetProperty("navMessageId").GetInt32().Should().Be(42);
        _ = element.GetProperty("navigationStack")[0].GetString().Should().Be("main");
    }

    [Fact]
    public async Task Save_WritesUserDataAsJson()
    {
        RedisSessionStore store = CreateStore();
        UserSession session = await store.GetOrCreateAsync(400);
        session.Set("city", "Moscow");
        session.Set("lang", "ru");

        await store.SaveAsync(session);

        string json = (await Db.StringGetAsync("bot:session:400"))!;
        _ = json.Should().NotBeNullOrEmpty();

        JsonElement element = JsonSerializer.Deserialize<JsonElement>(json);
        _ = element.TryGetProperty("userData", out JsonElement userData).Should().BeTrue();
        _ = userData.GetProperty("city").GetString().Should().Be("Moscow");
        _ = userData.GetProperty("lang").GetString().Should().Be("ru");
    }

    [Fact]
    public async Task Roundtrip_PreservesAllData()
    {
        RedisSessionStore store = CreateStore();
        UserSession original = await store.GetOrCreateAsync(500);
        original.CurrentScreen = "contact:share";
        original.NavMessageId = 99;
        original.CurrentMediaType = ScreenMediaType.Photo;
        original.NavigationStack.Add("main");
        original.NavigationStack.Add("settings");
        original.Set("age", "30");
        original.Set("city", "Berlin");
        await store.SaveAsync(original);

        UserSession restored = await store.GetOrCreateAsync(500);

        _ = restored.UserId.Should().Be(500);
        _ = restored.CurrentScreen.Should().Be("contact:share");
        _ = restored.NavMessageId.Should().Be(99);
        _ = restored.CurrentMediaType.Should().Be(ScreenMediaType.Photo);
        _ = restored.NavigationStack.Should().HaveCount(2);
        _ = restored.NavigationStack[0].Should().Be("main");
        _ = restored.NavigationStack[1].Should().Be("settings");
        _ = restored.GetString("age").Should().Be("30");
        _ = restored.GetString("city").Should().Be("Berlin");
        _ = restored.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public async Task Roundtrip_PreservesCreatedAt()
    {
        RedisSessionStore store = CreateStore();
        UserSession original = await store.GetOrCreateAsync(600);
        DateTime originalCreatedAt = original.CreatedAt;
        await store.SaveAsync(original);

        UserSession restored = await store.GetOrCreateAsync(600);

        _ = restored.CreatedAt.Should().BeCloseTo(originalCreatedAt, TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public async Task Save_UpdatesLastActivity()
    {
        RedisSessionStore store = CreateStore();
        UserSession session = await store.GetOrCreateAsync(700);
        DateTime firstActivity = session.LastActivity;

        await Task.Delay(50);

        await store.SaveAsync(session);

        _ = session.LastActivity.Should().BeAfter(firstActivity);

        UserSession restored = await store.GetOrCreateAsync(700);
        _ = restored.LastActivity.Should().BeAfter(firstActivity);
    }

    [Fact]
    public async Task RemovedKeys_DoNotPersist()
    {
        RedisSessionStore store = CreateStore();
        UserSession session = await store.GetOrCreateAsync(800);
        session.Set("temp_code", "1234");
        session.Set("city", "Moscow");
        await store.SaveAsync(session);

        session.Remove("temp_code");
        await store.SaveAsync(session);

        UserSession restored = await store.GetOrCreateAsync(800);
        _ = restored.Has("temp_code").Should().BeFalse();
        _ = restored.GetString("temp_code").Should().BeNull();
        _ = restored.GetString("city").Should().Be("Moscow");
        _ = restored.GetAll().Should().HaveCount(1);
    }

    [Fact]
    public async Task ClearedSession_PersistsCorrectly()
    {
        RedisSessionStore store = CreateStore();
        UserSession session = await store.GetOrCreateAsync(900);
        session.CurrentScreen = "main";
        session.NavMessageId = 10;
        session.Set("data", "value");
        await store.SaveAsync(session);

        session.Clear();
        await store.SaveAsync(session);

        UserSession restored = await store.GetOrCreateAsync(900);
        _ = restored.CurrentScreen.Should().BeNull();
        _ = restored.NavMessageId.Should().BeNull();
        _ = restored.GetAll().Should().BeEmpty();
    }

    [Fact]
    public async Task SessionTtl_SetsExpireOnKey()
    {
        RedisSessionStore store = CreateStore(sessionTtlMinutes: 5);
        UserSession session = await store.GetOrCreateAsync(1000);

        await store.SaveAsync(session);

        TimeSpan? ttl = await Db.KeyTimeToLiveAsync("bot:session:1000");
        _ = ttl.Should().NotBeNull();
        _ = ttl!.Value.TotalMinutes.Should().BeGreaterThan(4).And.BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task NoSessionTtl_KeyHasNoExpire()
    {
        RedisSessionStore store = CreateStore(sessionTtlMinutes: null);
        UserSession session = await store.GetOrCreateAsync(1100);

        await store.SaveAsync(session);

        TimeSpan? ttl = await Db.KeyTimeToLiveAsync("bot:session:1100");
        _ = ttl.Should().BeNull();
    }

    [Fact]
    public async Task MultipleSaves_OverwriteCleanly()
    {
        RedisSessionStore store = CreateStore();
        UserSession session = await store.GetOrCreateAsync(1200);

        session.Set("step", "1");
        session.CurrentScreen = "screen_v1";
        await store.SaveAsync(session);

        session.Set("step", "2");
        session.CurrentScreen = "screen_v2";
        await store.SaveAsync(session);

        UserSession restored = await store.GetOrCreateAsync(1200);

        _ = restored.GetString("step").Should().Be("2");
        _ = restored.CurrentScreen.Should().Be("screen_v2");
    }

    [Fact]
    public async Task DifferentUsers_IndependentSessions()
    {
        RedisSessionStore store = CreateStore();

        UserSession sessionA = await store.GetOrCreateAsync(1300);
        sessionA.Set("name", "Alice");
        await store.SaveAsync(sessionA);

        UserSession sessionB = await store.GetOrCreateAsync(1400);
        sessionB.Set("name", "Bob");
        await store.SaveAsync(sessionB);

        UserSession restoredA = await store.GetOrCreateAsync(1300);
        UserSession restoredB = await store.GetOrCreateAsync(1400);

        _ = restoredA.GetString("name").Should().Be("Alice");
        _ = restoredB.GetString("name").Should().Be("Bob");
    }

    [Fact]
    public async Task Save_StoresValueAsString_NotHash()
    {
        RedisSessionStore store = CreateStore();
        UserSession session = await store.GetOrCreateAsync(1500);
        session.Set("key1", "val1");
        await store.SaveAsync(session);

        StackExchange.Redis.RedisValue raw = await Db.StringGetAsync("bot:session:1500");

        _ = raw.IsNullOrEmpty.Should().BeFalse();
        _ = raw.ToString().Should().StartWith("{");
    }
}
