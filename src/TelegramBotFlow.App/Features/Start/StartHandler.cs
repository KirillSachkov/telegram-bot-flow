using TelegramBotFlow.App.Features.Help;
using TelegramBotFlow.App.Features.MainMenu;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Constants;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.App.Features.Start;

/// <summary>
/// Регистрирует стартовые команды бота и открывает базовые экраны навигации.
/// </summary>
public sealed class StartHandler : IBotEndpoint
{
    /// <summary>
    /// Добавляет маршруты <c>/start</c>, <c>/start {payload}</c> (deep link),
    /// <c>/help</c>, и обработчик <c>MyChatMember</c>.
    /// </summary>
    /// <param name="app">Экземпляр приложения бота для регистрации маршрутов.</param>
    public void MapEndpoint(BotApplication app)
    {
        app.MapCommand(BotCommands.START, (UpdateContext ctx) =>
        {
            ctx.Session?.Clear();
            return BotResults.NavigateTo<MainMenuScreen>();
        });

        // Deep link: /start <payload> — higher priority, matches when payload is present.
        app.MapDeepLink(BotCommands.START, (UpdateContext ctx) =>
        {
            string payload = ctx.CommandArgument!;
            ctx.Session?.Data.Set("referral", payload);
            return BotResults.NavigateToRoot<MainMenuScreen>();
        });

        app.MapCommand(BotCommands.HELP, BotResults.NavigateTo<HelpScreen>);

        // MyChatMember: fires when user blocks/unblocks the bot.
        // UserTrackingMiddleware already auto-marks blocked users;
        // this handler is for custom logic (logging, analytics, etc.).
        app.MapChatMember((UpdateContext ctx) =>
        {
            return Task.FromResult(BotResults.Empty());
        });
    }
}