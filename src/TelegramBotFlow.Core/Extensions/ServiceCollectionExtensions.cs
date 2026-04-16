using System.Reflection;
using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTelegramBotFlow(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BotConfiguration>(configuration.GetSection(BotConfiguration.SECTION_NAME));
        services.Configure<BotMessages>(configuration.GetSection("Bot:Messages"));

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
        services.AddScoped<INavigationService, NavigationService>();

        services.AddSingleton<ISessionStore, InMemorySessionStore>();
        services.AddSingleton<ISessionLockProvider, InMemorySessionLockProvider>();

        services.AddSingleton<InputHandlerRegistry>();
        services.AddScoped<PendingInputMiddleware>();

        services.AddScoped<ErrorHandlingMiddleware>();
        services.AddScoped<LoggingMiddleware>();
        services.AddScoped<PrivateChatOnlyMiddleware>();
        services.AddScoped<SessionMiddleware>();
        services.AddScoped<AccessPolicyMiddleware>();

        Channel<Update> updateChannel = Channel.CreateBounded<Update>(new BoundedChannelOptions(botConfig.UpdateChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = false
        });
        services.AddSingleton(updateChannel.Writer);
        services.AddSingleton(updateChannel.Reader);

        if (botConfig.Mode == BotMode.POLLING)
            services.AddHostedService<PollingService>();

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

    public static IServiceCollection AddWizards(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddSingleton<IWizardStore, InMemoryWizardStore>();
        services.AddScoped<IWizardLauncher, WizardLauncher>();
        services.AddScoped<WizardMiddleware>();

        WizardRegistry registry = new();

        foreach (Assembly assembly in assemblies)
        {
            foreach (Type wizardType in WizardRegistry.GetWizardTypes(assembly))
            {
                registry.Register(wizardType);
                services.TryAddScoped(wizardType);
            }
        }

        services.AddSingleton(registry);

        return services;
    }

    public static IServiceCollection AddScreens(this IServiceCollection services, Assembly assembly)
    {
        List<Type> screenTypes = [.. ScreenRegistry.GetScreenTypes(assembly)];

        foreach (Type screenType in screenTypes)
            services.TryAddScoped(screenType);

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