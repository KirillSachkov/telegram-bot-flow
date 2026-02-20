using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.IntegrationTests.Infrastructure;

namespace TelegramBotFlow.IntegrationTests.Application;

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
        HttpClient client = _factory.CreateClient();
        IServiceProvider services = _factory.Services;

        _ = client.Should().NotBeNull();
        _ = services.Should().NotBeNull();
    }

    [Fact]
    public void Should_Resolve_TelegramBotClient()
    {
        ITelegramBotClient? botClient = _factory.Services.GetService<ITelegramBotClient>();

        _ = botClient.Should().NotBeNull();
        _ = botClient.Should().BeSameAs(_factory.MockTelegramBotClient,
            "application should use mocked bot client");
    }

    [Fact]
    public void Should_Resolve_Core_Singleton_Services()
    {
        UpdateRouter? updateRouter = _factory.Services.GetService<UpdateRouter>();
        ISessionStore? sessionStore = _factory.Services.GetService<ISessionStore>();
        UpdatePipeline? updatePipeline = _factory.Services.GetService<UpdatePipeline>();

        _ = updateRouter.Should().NotBeNull("UpdateRouter should be registered");
        _ = sessionStore.Should().NotBeNull("ISessionStore should be registered");
        _ = updatePipeline.Should().NotBeNull("UpdatePipeline should be registered");
    }

    [Fact]
    public void Should_Resolve_All_Middlewares_As_Scoped()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IServiceProvider scopedServices = scope.ServiceProvider;

        ErrorHandlingMiddleware? errorHandling = scopedServices.GetService<ErrorHandlingMiddleware>();
        LoggingMiddleware? logging = scopedServices.GetService<LoggingMiddleware>();
        SessionMiddleware? session = scopedServices.GetService<SessionMiddleware>();

        _ = errorHandling.Should().NotBeNull("ErrorHandlingMiddleware should be registered");
        _ = logging.Should().NotBeNull("LoggingMiddleware should be registered");
        _ = session.Should().NotBeNull("SessionMiddleware should be registered");
    }

    [Fact]
    public void Singleton_Services_Should_Return_Same_Instance()
    {
        ITelegramBotClient bot1 = _factory.Services.GetRequiredService<ITelegramBotClient>();
        ITelegramBotClient bot2 = _factory.Services.GetRequiredService<ITelegramBotClient>();

        UpdateRouter router1 = _factory.Services.GetRequiredService<UpdateRouter>();
        UpdateRouter router2 = _factory.Services.GetRequiredService<UpdateRouter>();

        _ = bot1.Should().BeSameAs(bot2, "singleton should return same instance");
        _ = router1.Should().BeSameAs(router2, "singleton should return same instance");
    }

    [Fact]
    public void Scoped_Services_Should_Return_Different_Instances_Per_Scope()
    {
        using IServiceScope scope1 = _factory.Services.CreateScope();
        using IServiceScope scope2 = _factory.Services.CreateScope();

        ErrorHandlingMiddleware middleware1 = scope1.ServiceProvider.GetRequiredService<ErrorHandlingMiddleware>();
        ErrorHandlingMiddleware middleware2 = scope2.ServiceProvider.GetRequiredService<ErrorHandlingMiddleware>();

        _ = middleware1.Should().NotBeSameAs(middleware2,
            "scoped services should return different instances in different scopes");
    }

    [Fact]
    public void Should_Create_Multiple_Scopes_Without_Errors()
    {
        Action action = () =>
        {
            for (int i = 0; i < 10; i++)
            {
                using IServiceScope scope = _factory.Services.CreateScope();
                ErrorHandlingMiddleware errorHandling =
                    scope.ServiceProvider.GetRequiredService<ErrorHandlingMiddleware>();
                _ = errorHandling.Should().NotBeNull();
            }
        };

        _ = action.Should().NotThrow("creating multiple scopes should not cause errors");
    }

    [Fact]
    public void Should_Have_Configuration_Loaded()
    {
        Microsoft.Extensions.Configuration.IConfiguration? configuration =
            _factory.Services.GetService<Microsoft.Extensions.Configuration.IConfiguration>();

        _ = configuration.Should().NotBeNull();
        _ = configuration["Bot:Token"].Should().Be("fake-token-for-testing");
        _ = configuration["Bot:Mode"].Should().Be("Polling");
    }

    [Fact]
    public void MockTelegramBotClient_Should_Be_Available_And_Usable()
    {
        ITelegramBotClient mockBot = _factory.MockTelegramBotClient;
        ITelegramBotClient registeredBot = _factory.Services.GetRequiredService<ITelegramBotClient>();

        _ = mockBot.Should().NotBeNull();
        _ = registeredBot.Should().BeSameAs(mockBot);
    }
}
