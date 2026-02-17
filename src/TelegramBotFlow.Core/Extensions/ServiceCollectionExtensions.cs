using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Telegram.Bot;
using TelegramBotFlow.Core.Flows;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.Core.Throttling;

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
        services.AddScoped<ThrottlingMiddleware>();

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

    /// <summary>
    /// Добавляет rate limiting для защиты от флуда.
    /// Использует System.Threading.RateLimiting с in-memory хранилищем.
    /// </summary>
    public static IServiceCollection AddThrottling(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Throttling")
    {
        services.Configure<ThrottlingOptions>(configuration.GetSection(sectionName));

        services.AddSingleton<PartitionedRateLimiter<long>>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ThrottlingOptions>>().Value;

            return PartitionedRateLimiter.Create<long, long>(userId =>
            {
                // Для каждого UserId создаём свой SlidingWindowRateLimiter
                return RateLimitPartition.GetSlidingWindowLimiter(userId, _ =>
                    new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.WindowSeconds),
                        SegmentsPerWindow = options.SegmentsPerWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // Не ставим в очередь, сразу отклоняем
                    });
            });
        });

        return services;
    }

    /// <summary>
    /// Добавляет rate limiting с кастомной конфигурацией.
    /// </summary>
    public static IServiceCollection AddThrottling(
        this IServiceCollection services,
        Action<ThrottlingOptions> configure)
    {
        services.Configure(configure);

        services.AddSingleton<PartitionedRateLimiter<long>>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ThrottlingOptions>>().Value;

            return PartitionedRateLimiter.Create<long, long>(userId =>
            {
                return RateLimitPartition.GetSlidingWindowLimiter(userId, _ =>
                    new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.WindowSeconds),
                        SegmentsPerWindow = options.SegmentsPerWindow,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }
}
