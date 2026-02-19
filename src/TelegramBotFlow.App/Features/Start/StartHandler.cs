using TelegramBotFlow.App.Features.Help;
using TelegramBotFlow.App.Features.MainMenu;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Features.Start;

/// <summary>
/// Регистрирует стартовые команды бота и открывает базовые экраны навигации.
/// </summary>
public sealed class StartHandler : IBotEndpoint
{
    /// <summary>
    /// Добавляет маршруты <c>/start</c> и <c>/help</c>.
    /// </summary>
    /// <param name="app">Экземпляр приложения бота для регистрации маршрутов.</param>
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
