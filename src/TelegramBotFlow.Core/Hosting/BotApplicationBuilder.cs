using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Extensions;

namespace TelegramBotFlow.Core.Hosting;

public sealed class BotApplicationBuilder
{
    public WebApplicationBuilder WebAppBuilder { get; }
    public IServiceCollection Services => WebAppBuilder.Services;
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
            Services.AddBotEndpoints(entryAssembly);
            Services.AddScreens(entryAssembly);
        }

        return BotApplication.Build(this);
    }
}
