using Microsoft.Extensions.Logging;
using TelegramBotFlow.Core.Pipeline;

namespace TelegramBotFlow.Core.Routing;

public sealed class UpdateRouter
{
    private readonly List<RouteEntry> _routes = [];
    private readonly ILogger<UpdateRouter> _logger;
    private UpdateDelegate? _fallbackHandler;

    public UpdateRouter(ILogger<UpdateRouter> logger)
    {
        _logger = logger;
    }

    public void AddRoute(RouteEntry route) => _routes.Add(route);

    public void SetFallback(UpdateDelegate handler) => _fallbackHandler = handler;

    public UpdateDelegate BuildTerminal()
    {
        List<RouteEntry> sorted = _routes
            .OrderBy(r => r.Priority)
            .ToList();

        return async context =>
        {
            foreach (RouteEntry route in sorted)
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

            if (_fallbackHandler is not null)
            {
                _logger.LogDebug(
                    "No route matched, invoking fallback for user {UserId}",
                    context.UserId);

                await _fallbackHandler(context);
                return;
            }

            _logger.LogDebug(
                "No route matched for update {UpdateType} from user {UserId}",
                context.UpdateType,
                context.UserId);
        };
    }
}
