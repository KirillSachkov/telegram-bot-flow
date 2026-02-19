using TelegramBotFlow.App.Features.MainMenu;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Features.Navigation;

/// <summary>
/// Обрабатывает callback-навигацию <c>nav:*</c> между экранами.
/// </summary>
public sealed class NavigationHandler : IBotEndpoint
{
    /// <summary>
    /// Регистрирует групповой callback-маршрут для навигационных действий.
    /// </summary>
    /// <param name="app">Экземпляр приложения бота для регистрации маршрутов.</param>
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
