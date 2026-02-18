using TelegramBotFlow.App.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Handlers;

public sealed class StartHandler : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapCommand("/start", async (UpdateContext ctx, IScreenNavigator navigator) =>
        {
            ctx.Session?.Clear();
            await navigator.NavigateToAsync<MainMenuScreen>(ctx);
        });

        app.MapCommand("/help", async (UpdateContext ctx, IScreenNavigator navigator) =>
        {
            await navigator.NavigateToAsync<HelpScreen>(ctx);
        });
    }
}
