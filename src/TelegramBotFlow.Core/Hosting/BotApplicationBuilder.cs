using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
}
