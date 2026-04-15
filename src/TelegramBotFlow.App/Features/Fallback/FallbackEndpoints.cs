using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.App.Features.Fallback;

/// <summary>
/// Регистрирует fallback-обработчик для обновлений без совпавшего маршрута.
/// </summary>
public sealed class FallbackEndpoints : IBotEndpoint
{
    /// <summary>
    /// Добавляет fallback-маршрут с подсказкой пользователю.
    /// </summary>
    /// <param name="app">Экземпляр приложения бота для регистрации маршрутов.</param>
    public void MapEndpoint(BotApplication app) =>
        app.MapFallback(async (UpdateContext ctx, IUpdateResponder responder) =>
        {
            await responder.ReplyAsync(ctx, "Не понимаю. Нажмите кнопку в меню или /start");
            return BotResults.Empty();
        });
}
