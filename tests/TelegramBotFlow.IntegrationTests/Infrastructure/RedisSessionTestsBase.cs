using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.IntegrationTests.Infrastructure;

/// <summary>
/// Базовый класс для интеграционных тестов Redis-сессий.
/// Очищает Redis перед каждым тестом.
/// </summary>
[Collection(nameof(RedisIntegrationTests))]
public abstract class RedisSessionTestsBase : IAsyncLifetime
{
    private readonly RedisFixture _fixture;

    protected RedisSessionTestsBase(RedisFixture fixture)
    {
        _fixture = fixture;
    }

    protected IConnectionMultiplexer Redis => _fixture.Connection;

    protected IDatabase Db => Redis.GetDatabase();

    protected RedisSessionStore CreateStore(int? sessionTtlMinutes = null)
    {
        var options = Options.Create(new RedisSessionOptions
        {
            SessionTtlMinutes = sessionTtlMinutes
        });

        return new RedisSessionStore(Redis, options);
    }

    public async Task InitializeAsync()
    {
        var server = Redis.GetServer(Redis.GetEndPoints().First());
        await server.FlushDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
