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
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramBotFlow(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = services.Configure<BotConfiguration>(configuration.GetSection(BotConfiguration.SECTION_NAME));

        BotConfiguration botConfig = configuration.GetSection(BotConfiguration.SECTION_NAME).Get<BotConfiguration>()
                                     ?? throw new InvalidOperationException(
                                         $"Bot configuration section '{BotConfiguration.SECTION_NAME}' is missing or invalid.");

        _ = services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(botConfig.Token));

        _ = services.AddSingleton<PipelineHolder>();
        _ = services.AddSingleton(sp => sp.GetRequiredService<PipelineHolder>().Pipeline);

        _ = services.AddSingleton<UpdateRouter>();
        services.TryAddSingleton<ScreenRegistry>();
        _ = services.AddScoped<IUpdateResponder, UpdateResponder>();
        _ = services.AddScoped<IUserAccessPolicy, BotConfigurationUserAccessPolicy>();
        _ = services.AddScoped<IScreenMessageRenderer, ScreenMessageRenderer>();
        _ = services.AddScoped<ScreenManager>();
        _ = services.AddScoped<IScreenNavigator, ScreenNavigator>();

        _ = services.AddSingleton<ISessionStore, InMemorySessionStore>();
        _ = services.AddSingleton<ISessionLockProvider, InMemorySessionLockProvider>();

        _ = services.AddSingleton<InputHandlerRegistry>();
        _ = services.AddScoped<PendingInputMiddleware>();

        _ = services.AddScoped<ErrorHandlingMiddleware>();
        _ = services.AddScoped<LoggingMiddleware>();
        _ = services.AddScoped<PrivateChatOnlyMiddleware>();
        _ = services.AddScoped<SessionMiddleware>();
        _ = services.AddScoped<AccessPolicyMiddleware>();

        // Register Channel for Update processing
        Channel<Update> updateChannel = Channel.CreateBounded<Update>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait, SingleWriter = true, SingleReader = false
        });
        _ = services.AddSingleton(updateChannel.Writer);
        _ = services.AddSingleton(updateChannel.Reader);

        if (botConfig.Mode == BotMode.POLLING)
        {
            _ = services.AddHostedService<PollingService>();
        }

        _ = services.AddHostedService<UpdateProcessingWorker>();

        return services;
    }

    public static IServiceCollection AddSessionStore<TStore>(this IServiceCollection services)
        where TStore : class, ISessionStore
    {
        _ = services.RemoveAll<ISessionStore>();
        _ = services.AddSingleton<ISessionStore, TStore>();

        return services;
    }

    public static IServiceCollection AddRedisSessionStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = services.Configure<RedisSessionOptions>(configuration.GetSection(RedisSessionOptions.SECTION_NAME));

        RedisSessionOptions options =
            configuration.GetSection(RedisSessionOptions.SECTION_NAME).Get<RedisSessionOptions>()
            ?? new RedisSessionOptions();

        _ = services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(options.ConnectionString));

        _ = services.RemoveAll<ISessionStore>();
        _ = services.AddSingleton<ISessionStore, RedisSessionStore>();
        _ = services.AddSingleton<ISessionLockProvider, InMemorySessionLockProvider>(); // TODO: Implement Redis version

        return services;
    }

    public static IServiceCollection AddWizards(this IServiceCollection services, params Assembly[] assemblies)
    {
        _ = services.AddSingleton<IWizardStore, InMemoryWizardStore>();
        _ = services.AddScoped<WizardMiddleware>();

        var registry = new WizardRegistry();

        foreach (Assembly assembly in assemblies)
        {
            foreach (Type wizardType in WizardRegistry.GetWizardTypes(assembly))
            {
                registry.Register(wizardType);
                services.TryAddScoped(wizardType);
            }
        }

        _ = services.AddSingleton(registry);

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
        _ = services.RemoveAll<ScreenRegistry>();
        _ = services.AddSingleton(_ =>
        {
            var registry = new ScreenRegistry();
            registry.RegisterFromAssembly(assembly);
            return registry;
        });

        return services;
    }
}
