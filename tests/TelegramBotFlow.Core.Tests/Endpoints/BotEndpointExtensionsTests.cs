using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.Core.Tests.Endpoints;

public class BotEndpointExtensionsTests
{
    [Fact]
    public void AddBotEndpoints_ShouldRegisterAllEndpointsFromAssembly()
    {
        var services = new ServiceCollection();

        _ = services.AddBotEndpoints(typeof(TestEndpointA).Assembly);

        ServiceProvider provider = services.BuildServiceProvider();
        var endpoints = provider.GetServices<IBotEndpoint>().ToList();

        _ = endpoints.Should().HaveCount(2);
        _ = endpoints.Should().ContainSingle(e => e is TestEndpointA);
        _ = endpoints.Should().ContainSingle(e => e is TestEndpointB);
    }

    [Fact]
    public void AddBotEndpoints_ShouldIgnoreAbstractClasses()
    {
        var services = new ServiceCollection();

        _ = services.AddBotEndpoints(typeof(AbstractEndpoint).Assembly);

        ServiceProvider provider = services.BuildServiceProvider();
        var endpoints = provider.GetServices<IBotEndpoint>().ToList();

        _ = endpoints.Should().NotContain(e => e.GetType() == typeof(AbstractEndpoint));
    }

    [Fact]
    public void AddBotEndpoints_ShouldNotDuplicateOnSecondCall()
    {
        var services = new ServiceCollection();

        _ = services.AddBotEndpoints(typeof(TestEndpointA).Assembly);
        _ = services.AddBotEndpoints(typeof(TestEndpointA).Assembly);

        ServiceProvider provider = services.BuildServiceProvider();
        var endpoints = provider.GetServices<IBotEndpoint>().ToList();

        _ = endpoints.Should().HaveCount(2);
    }
}

public sealed class TestEndpointA : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
    }
}

public sealed class TestEndpointB : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
    }
}

public abstract class AbstractEndpoint : IBotEndpoint
{
    public abstract void MapEndpoint(BotApplication app);
}
