using TelegramBotFlow.App.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Handlers;

public sealed class NavigationHandler : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapCallbackGroup("nav", async (
            UpdateContext ctx,
            string screenId,
            IUpdateResponder responder,
            IScreenNavigator navigator) =>
        {
            await responder.AnswerCallbackAsync(ctx);

            if (screenId == "back")
                await navigator.GoBackAsync(ctx);
            else if (screenId == "close")
                await navigator.RefreshScreenAsync(ctx);
            else if (screenId == "menu")
            {
                ctx.Session?.Clear();
                await navigator.NavigateToAsync<MainMenuScreen>(ctx);
            }
            else
                await navigator.NavigateToAsync(ctx, screenId);
        });
    }
}
