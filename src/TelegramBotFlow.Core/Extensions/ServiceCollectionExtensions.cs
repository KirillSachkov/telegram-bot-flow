using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Telegram.Bot;
using TelegramBotFlow.Core.Flows;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramBotFlow(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BotConfiguration>(configuration.GetSection(BotConfiguration.SectionName));

        var botConfig = configuration.GetSection(BotConfiguration.SectionName).Get<BotConfiguration>()
                        ?? throw new InvalidOperationException(
                            $"Bot configuration section '{BotConfiguration.SectionName}' is missing or invalid.");

        services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(botConfig.Token));

        services.AddSingleton<PipelineHolder>();
        services.AddSingleton<UpdatePipeline>(sp => sp.GetRequiredService<PipelineHolder>().Pipeline);

        services.AddSingleton<UpdateRouter>();
        services.AddSingleton<FlowManager>();

        services.AddSingleton<ISessionStore, InMemorySessionStore>();

        services.AddScoped<ErrorHandlingMiddleware>();
        services.AddScoped<LoggingMiddleware>();
        services.AddScoped<SessionMiddleware>();
        services.AddScoped<FlowMiddleware>();

        if (botConfig.Mode == BotMode.Polling)
        {
            services.AddHostedService<PollingService>();
        }

        return services;
    }

    public static IServiceCollection AddSessionStore<TStore>(this IServiceCollection services)
        where TStore : class, ISessionStore
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISessionStore));
        if (descriptor is not null)
            services.Remove(descriptor);

        services.AddSingleton<ISessionStore, TStore>();

        return services;
    }

    /// <summary>
    /// Регистрирует Redis как хранилище сессий.
    /// Читает настройки из секции "Redis" в конфигурации.
    /// </summary>
    public static IServiceCollection AddRedisSessionStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RedisSessionOptions>(configuration.GetSection(RedisSessionOptions.SectionName));

        var options = configuration.GetSection(RedisSessionOptions.SectionName).Get<RedisSessionOptions>()
                      ?? new RedisSessionOptions();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(options.ConnectionString));

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISessionStore));
        if (descriptor is not null)
            services.Remove(descriptor);

        services.AddSingleton<ISessionStore, RedisSessionStore>();

        return services;
    }
}
