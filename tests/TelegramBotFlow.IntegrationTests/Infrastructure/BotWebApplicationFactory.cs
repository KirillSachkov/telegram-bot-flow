using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Telegram.Bot;

namespace TelegramBotFlow.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory для интеграционных тестов с мокированным Telegram API.
/// </summary>
public class BotWebApplicationFactory : WebApplicationFactory<Program>
{
    public ITelegramBotClient MockTelegramBotClient { get; }

    public BotWebApplicationFactory()
    {
        MockTelegramBotClient = Substitute.For<ITelegramBotClient>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bot:Token"] = "fake-token-for-testing",
                ["Bot:Mode"] = "Polling"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Подменяем ITelegramBotClient на мок
            services.RemoveAll<ITelegramBotClient>();
            services.AddSingleton(MockTelegramBotClient);

            // Отключаем PollingService для тестов
            services.RemoveAll(typeof(Microsoft.Extensions.Hosting.IHostedService));
        });
    }
}
