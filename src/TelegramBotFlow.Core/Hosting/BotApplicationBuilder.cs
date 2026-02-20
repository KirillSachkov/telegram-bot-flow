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
    /// Automatically discovers and registers <see cref="IBotEndpoint"/> implementations
    /// and <see cref="Screens.IScreen"/> implementations from the entry assembly.
    /// </summary>
    public BotApplication Build()
    {
        Assembly? entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is not null)
        {
            _ = Services.AddBotEndpoints(entryAssembly);
            _ = Services.AddScreens(entryAssembly);
            _ = Services.AddWizards(entryAssembly);
        }

        return BotApplication.Build(this);
    }
}
