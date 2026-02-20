using TelegramBotFlow.App.Features.Help;
using TelegramBotFlow.App.Features.MainMenu;
using TelegramBotFlow.Core.Constants;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;

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
        app.MapCommand(BotCommands.START, (UpdateContext ctx) =>
        {
            ctx.Session?.Clear();
            return BotResults.NavigateTo<MainMenuScreen>();
        });

        app.MapCommand(BotCommands.HELP, BotResults.NavigateTo<HelpScreen>);
    }
}
