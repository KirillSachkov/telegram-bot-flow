using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.App.Endpoints.General;

/// <summary>
/// /help — справка по командам и UI-элементам.
/// Callback "help" — подсказка из InlineKeyboard в /start.
/// </summary>
public sealed class HelpCommand : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapCommand("/help", async ctx =>
        {
            const string text = "Доступные команды:\n\n" +
                                "/start — главное меню (InlineKeyboard)\n" +
                                "/help — эта справка\n" +
                                "/settings — настройки (ReplyKeyboard)\n" +
                                "/register — регистрация (Flow + ReplyKeyboard)\n\n" +
                                "Примеры UI-элементов в этом боте:\n\n" +
                                "InlineKeyboard — кнопки под сообщением\n" +
                                "  Button(text, callbackData) — callback-кнопка\n" +
                                "  Url(text, url) — ссылка\n" +
                                "  SingleButton / SingleUrl — shortcut для одной кнопки\n\n" +
                                "ReplyKeyboard — кнопки вместо клавиатуры\n" +
                                "  Button(text) — текстовая кнопка\n" +
                                "  RequestContact(text) — запрос телефона\n" +
                                "  RequestLocation(text) — запрос геолокации\n" +
                                "  OneTime() — скрыть после нажатия\n" +
                                "  Remove() — убрать клавиатуру";

            await ctx.ReplyAsync(text,
                InlineKeyboard.SingleUrl("Telegram Bot API Docs", "https://core.telegram.org/bots/api"));
        });

        app.MapCallback("help", async ctx =>
        {
            await ctx.AnswerCallbackAsync();
            await ctx.ReplyAsync("Используйте /help для полной справки.");
        });
    }
}
