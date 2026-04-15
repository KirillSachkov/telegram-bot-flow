using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Extensions;

namespace TelegramBotFlow.Core.Hosting;

/// <summary>
/// Конфигуратор и фабрика экземпляра <see cref="BotApplication"/>.
/// </summary>
public sealed class BotApplicationBuilder
{
    /// <summary>
    /// Базовый web-builder приложения.
    /// </summary>
    public WebApplicationBuilder WebAppBuilder { get; }

    /// <summary>
    /// Коллекция сервисов DI-контейнера.
    /// </summary>
    public IServiceCollection Services => WebAppBuilder.Services;

    /// <summary>
    /// Конфигурация приложения.
    /// </summary>
    public ConfigurationManager Configuration => WebAppBuilder.Configuration;

    internal BotApplicationBuilder(string[] args)
    {
        WebAppBuilder = WebApplication.CreateBuilder(args);
    }

    /// <summary>
    /// Builds the <see cref="BotApplication"/>.
    /// Automatically discovers and registers <see cref="IBotEndpoint"/> implementations,
    /// <see cref="Screens.IScreen"/> implementations and <c>IBotWizard</c> implementations
    /// from the entry assembly — only if that assembly actually contains such types.
    ///
    /// This check prevents overwriting explicit service registrations when the application is
    /// started from a test host, where <see cref="Assembly.GetEntryAssembly"/> returns the
    /// test runner (e.g. xunit) rather than the application assembly.
    /// </summary>
    public BotApplication Build()
    {
        Assembly? entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is not null)
        {
            // AddBotEndpoints is safe to call unconditionally (uses TryAddEnumerable)
            Services.AddBotEndpoints(entryAssembly);

            // Only register screens if the entry assembly actually contains IScreen types.
            // Without this check, AddScreens(testRunnerAssembly) would replace the explicitly
            // registered ScreenRegistry with an empty one.
            if (Screens.ScreenRegistry.GetScreenTypes(entryAssembly).Any())
                Services.AddScreens(entryAssembly);

            // Same guard for wizards.
            if (Wizards.WizardRegistry.GetWizardTypes(entryAssembly).Any())
                Services.AddWizards(entryAssembly);
        }

        return BotApplication.Build(this);
    }
}