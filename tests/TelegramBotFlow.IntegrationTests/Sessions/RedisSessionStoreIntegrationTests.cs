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

        session.UserId.Should().Be(100);

        bool exists = await Db.KeyExistsAsync("bot:session:100");
        exists.Should().BeTrue();
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

        restored.UserId.Should().Be(200);
        restored.GetString("name").Should().Be("Alice");
        restored.CurrentScreen.Should().Be("main");
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
        json.Should().NotBeNullOrEmpty();

        JsonElement element = JsonSerializer.Deserialize<JsonElement>(json);
        element.TryGetProperty("createdAt", out _).Should().BeTrue();
        element.TryGetProperty("lastActivity", out _).Should().BeTrue();
        element.GetProperty("currentScreen").GetString().Should().Be("settings:main");
        element.GetProperty("navMessageId").GetInt32().Should().Be(42);
        element.GetProperty("navigationStack")[0].GetString().Should().Be("main");
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
        json.Should().NotBeNullOrEmpty();

        JsonElement element = JsonSerializer.Deserialize<JsonElement>(json);
        element.TryGetProperty("userData", out JsonElement userData).Should().BeTrue();
        userData.GetProperty("city").GetString().Should().Be("Moscow");
        userData.GetProperty("lang").GetString().Should().Be("ru");
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

        restored.UserId.Should().Be(500);
        restored.CurrentScreen.Should().Be("contact:share");
        restored.NavMessageId.Should().Be(99);
        restored.CurrentMediaType.Should().Be(ScreenMediaType.Photo);
        restored.NavigationStack.Should().HaveCount(2);
        restored.NavigationStack[0].Should().Be("main");
        restored.NavigationStack[1].Should().Be("settings");
        restored.GetString("age").Should().Be("30");
        restored.GetString("city").Should().Be("Berlin");
        restored.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public async Task Roundtrip_PreservesCreatedAt()
    {
        RedisSessionStore store = CreateStore();
        UserSession original = await store.GetOrCreateAsync(600);
        DateTime originalCreatedAt = original.CreatedAt;
        await store.SaveAsync(original);

        UserSession restored = await store.GetOrCreateAsync(600);

        restored.CreatedAt.Should().BeCloseTo(originalCreatedAt, TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public async Task Save_UpdatesLastActivity()
    {
        RedisSessionStore store = CreateStore();
        UserSession session = await store.GetOrCreateAsync(700);
        DateTime firstActivity = session.LastActivity;

        await Task.Delay(50);

        await store.SaveAsync(session);

        session.LastActivity.Should().BeAfter(firstActivity);

        UserSession restored = await store.GetOrCreateAsync(700);
        restored.LastActivity.Should().BeAfter(firstActivity);
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
        restored.Has("temp_code").Should().BeFalse();
        restored.GetString("temp_code").Should().BeNull();
        restored.GetString("city").Should().Be("Moscow");
        restored.GetAll().Should().HaveCount(1);
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
        restored.CurrentScreen.Should().BeNull();
        restored.NavMessageId.Should().BeNull();
        restored.GetAll().Should().BeEmpty();
    }

    [Fact]
    public async Task SessionTtl_SetsExpireOnKey()
    {
        RedisSessionStore store = CreateStore(sessionTtlMinutes: 5);
        UserSession session = await store.GetOrCreateAsync(1000);

        await store.SaveAsync(session);

        TimeSpan? ttl = await Db.KeyTimeToLiveAsync("bot:session:1000");
        ttl.Should().NotBeNull();
        ttl!.Value.TotalMinutes.Should().BeGreaterThan(4).And.BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task NoSessionTtl_KeyHasNoExpire()
    {
        RedisSessionStore store = CreateStore(sessionTtlMinutes: null);
        UserSession session = await store.GetOrCreateAsync(1100);

        await store.SaveAsync(session);

        TimeSpan? ttl = await Db.KeyTimeToLiveAsync("bot:session:1100");
        ttl.Should().BeNull();
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

        restored.GetString("step").Should().Be("2");
        restored.CurrentScreen.Should().Be("screen_v2");
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

        restoredA.GetString("name").Should().Be("Alice");
        restoredB.GetString("name").Should().Be("Bob");
    }

    [Fact]
    public async Task Save_StoresValueAsString_NotHash()
    {
        RedisSessionStore store = CreateStore();
        UserSession session = await store.GetOrCreateAsync(1500);
        session.Set("key1", "val1");
        await store.SaveAsync(session);

        StackExchange.Redis.RedisValue raw = await Db.StringGetAsync("bot:session:1500");

        raw.IsNullOrEmpty.Should().BeFalse();
        raw.ToString().Should().StartWith("{");
    }
}
