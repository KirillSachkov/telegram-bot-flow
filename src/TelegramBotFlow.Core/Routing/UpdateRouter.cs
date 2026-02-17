using Microsoft.Extensions.Logging;
using TelegramBotFlow.Core.Pipeline;

namespace TelegramBotFlow.Core.Routing;

public sealed class UpdateRouter
{
    private readonly List<RouteEntry> _routes = [];
    private readonly ILogger<UpdateRouter> _logger;

    public UpdateRouter(ILogger<UpdateRouter> logger)
    {
        _logger = logger;
    }

    public void AddRoute(RouteEntry route) => _routes.Add(route);

    public UpdateDelegate BuildTerminal()
    {
        return async context =>
        {
            foreach (var route in _routes)
            {
                if (!route.Matches(context))
                    continue;

                _logger.LogDebug(
                    "Matched route {RouteType} {Pattern} for user {UserId}",
                    route.Type,
                    route.Pattern ?? "(predicate)",
                    context.UserId);

                await route.Handler(context);
                return;
            }

            _logger.LogDebug(
                "No route matched for update {UpdateType} from user {UserId}",
                context.UpdateType,
                context.UserId);
        };
    }
}
