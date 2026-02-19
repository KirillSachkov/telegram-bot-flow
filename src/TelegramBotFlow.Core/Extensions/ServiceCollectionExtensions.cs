using System.Reflection;
using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.Core.Sessions.Redis;

namespace TelegramBotFlow.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramBotFlow(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BotConfiguration>(configuration.GetSection(BotConfiguration.SECTION_NAME));

        BotConfiguration botConfig = configuration.GetSection(BotConfiguration.SECTION_NAME).Get<BotConfiguration>()
                                     ?? throw new InvalidOperationException(
                                         $"Bot configuration section '{BotConfiguration.SECTION_NAME}' is missing or invalid.");

        services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(botConfig.Token));

        services.AddSingleton<PipelineHolder>();
        services.AddSingleton(sp => sp.GetRequiredService<PipelineHolder>().Pipeline);

        services.AddSingleton<UpdateRouter>();
        services.TryAddSingleton<ScreenRegistry>();
        services.AddScoped<IUpdateResponder, UpdateResponder>();
        services.AddScoped<IUserAccessPolicy, BotConfigurationUserAccessPolicy>();
        services.AddScoped<IScreenMessageRenderer, ScreenMessageRenderer>();
        services.AddScoped<ScreenManager>();
        services.AddScoped<IScreenNavigator, ScreenNavigator>();

        services.AddSingleton<ISessionStore, InMemorySessionStore>();
        services.AddSingleton<ISessionLockProvider, InMemorySessionLockProvider>();

        services.AddSingleton<InputHandlerRegistry>();
        services.AddScoped<PendingInputMiddleware>();

        services.AddScoped<ErrorHandlingMiddleware>();
        services.AddScoped<LoggingMiddleware>();
        services.AddScoped<PrivateChatOnlyMiddleware>();
        services.AddScoped<SessionMiddleware>();
        services.AddScoped<AccessPolicyMiddleware>();

        // Register Channel for Update processing
        Channel<Update> updateChannel = Channel.CreateBounded<Update>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait, SingleWriter = true, SingleReader = false
        });
        services.AddSingleton(updateChannel.Writer);
        services.AddSingleton(updateChannel.Reader);

        if (botConfig.Mode == BotMode.POLLING)
        {
            services.AddHostedService<PollingService>();
        }

        services.AddHostedService<UpdateProcessingWorker>();

        return services;
    }

    public static IServiceCollection AddSessionStore<TStore>(this IServiceCollection services)
        where TStore : class, ISessionStore
    {
        services.RemoveAll<ISessionStore>();
        services.AddSingleton<ISessionStore, TStore>();

        return services;
    }

    public static IServiceCollection AddRedisSessionStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RedisSessionOptions>(configuration.GetSection(RedisSessionOptions.SECTION_NAME));

        RedisSessionOptions options =
            configuration.GetSection(RedisSessionOptions.SECTION_NAME).Get<RedisSessionOptions>()
            ?? new RedisSessionOptions();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(options.ConnectionString));

        services.RemoveAll<ISessionStore>();
        services.AddSingleton<ISessionStore, RedisSessionStore>();
        services.AddSingleton<ISessionLockProvider, InMemorySessionLockProvider>(); // TODO: Implement Redis version

        return services;
    }

    public static IServiceCollection AddScreens(this IServiceCollection services, Assembly assembly)
    {
        List<Type> screenTypes = [.. ScreenRegistry.GetScreenTypes(assembly)];

        foreach (Type screenType in screenTypes)
            services.TryAddScoped(screenType);

        // Replace any existing ScreenRegistry registration with a factory
        // that populates the registry from the discovered types.
        // This avoids BuildServiceProvider() and works regardless of call order
        // relative to AddTelegramBotFlow.
        services.RemoveAll<ScreenRegistry>();
        services.AddSingleton(_ =>
        {
            var registry = new ScreenRegistry();
            registry.RegisterFromAssembly(assembly);
            return registry;
        });

        return services;
    }
}
