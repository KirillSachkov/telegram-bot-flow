using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using TelegramBotFlow.Core.Flows;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.IntegrationTests.Infrastructure;

namespace TelegramBotFlow.IntegrationTests.Application;

/// <summary>
/// Интеграционные тесты запуска приложения и разрешения зависимостей.
/// </summary>
[Collection(nameof(BotApplicationTests))]
public class ApplicationStartupTests : IClassFixture<BotWebApplicationFactory>
{
    private readonly BotWebApplicationFactory _factory;

    public ApplicationStartupTests(BotWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Application_Should_Start_Successfully()
    {
        // Arrange & Act
        var client = _factory.CreateClient();
        var services = _factory.Services;

        // Assert
        client.Should().NotBeNull();
        services.Should().NotBeNull();
    }

    [Fact]
    public void Should_Resolve_TelegramBotClient()
    {
        // Arrange & Act
        var botClient = _factory.Services.GetService<ITelegramBotClient>();

        // Assert
        botClient.Should().NotBeNull();
        botClient.Should().BeSameAs(_factory.MockTelegramBotClient, 
            "application should use mocked bot client");
    }

    [Fact]
    public void Should_Resolve_Core_Singleton_Services()
    {
        // Arrange & Act
        var updateRouter = _factory.Services.GetService<UpdateRouter>();
        var flowManager = _factory.Services.GetService<FlowManager>();
        var sessionStore = _factory.Services.GetService<ISessionStore>();
        var updatePipeline = _factory.Services.GetService<UpdatePipeline>();

        // Assert
        updateRouter.Should().NotBeNull("UpdateRouter should be registered");
        flowManager.Should().NotBeNull("FlowManager should be registered");
        sessionStore.Should().NotBeNull("ISessionStore should be registered");
        updatePipeline.Should().NotBeNull("UpdatePipeline should be registered");
    }

    [Fact]
    public void Should_Resolve_All_Middlewares_As_Scoped()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        // Act
        var errorHandling = scopedServices.GetService<ErrorHandlingMiddleware>();
        var logging = scopedServices.GetService<LoggingMiddleware>();
        var session = scopedServices.GetService<SessionMiddleware>();
        var flow = scopedServices.GetService<FlowMiddleware>();

        // Assert
        errorHandling.Should().NotBeNull("ErrorHandlingMiddleware should be registered");
        logging.Should().NotBeNull("LoggingMiddleware should be registered");
        session.Should().NotBeNull("SessionMiddleware should be registered");
        flow.Should().NotBeNull("FlowMiddleware should be registered");
    }

    [Fact]
    public void Singleton_Services_Should_Return_Same_Instance()
    {
        // Arrange & Act
        var bot1 = _factory.Services.GetRequiredService<ITelegramBotClient>();
        var bot2 = _factory.Services.GetRequiredService<ITelegramBotClient>();

        var router1 = _factory.Services.GetRequiredService<UpdateRouter>();
        var router2 = _factory.Services.GetRequiredService<UpdateRouter>();

        // Assert
        bot1.Should().BeSameAs(bot2, "singleton should return same instance");
        router1.Should().BeSameAs(router2, "singleton should return same instance");
    }

    [Fact]
    public void Scoped_Services_Should_Return_Different_Instances_Per_Scope()
    {
        // Arrange
        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();

        // Act
        var middleware1 = scope1.ServiceProvider.GetRequiredService<ErrorHandlingMiddleware>();
        var middleware2 = scope2.ServiceProvider.GetRequiredService<ErrorHandlingMiddleware>();

        // Assert
        middleware1.Should().NotBeSameAs(middleware2, 
            "scoped services should return different instances in different scopes");
    }

    [Fact]
    public void Should_Create_Multiple_Scopes_Without_Errors()
    {
        // Arrange & Act
        var action = () =>
        {
            for (int i = 0; i < 10; i++)
            {
                using var scope = _factory.Services.CreateScope();
                var errorHandling = scope.ServiceProvider.GetRequiredService<ErrorHandlingMiddleware>();
                errorHandling.Should().NotBeNull();
            }
        };

        // Assert
        action.Should().NotThrow("creating multiple scopes should not cause errors");
    }

    [Fact]
    public void Should_Have_Configuration_Loaded()
    {
        // Arrange & Act
        var configuration = _factory.Services.GetService<Microsoft.Extensions.Configuration.IConfiguration>();

        // Assert
        configuration.Should().NotBeNull();
        configuration["Bot:Token"].Should().Be("fake-token-for-testing");
        configuration["Bot:Mode"].Should().Be("Polling");
    }

    [Fact]
    public void MockTelegramBotClient_Should_Be_Available_And_Usable()
    {
        // Arrange & Act
        var mockBot = _factory.MockTelegramBotClient;
        var registeredBot = _factory.Services.GetRequiredService<ITelegramBotClient>();

        // Assert
        mockBot.Should().NotBeNull();
        registeredBot.Should().BeSameAs(mockBot);
    }
}
