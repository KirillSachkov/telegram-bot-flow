using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.App.Endpoints.General;

/// <summary>
/// /start — главное меню с InlineKeyboard.
/// </summary>
public sealed class StartCommand : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapCommand("/start", async ctx =>
        {
            var keyboard = new InlineKeyboard()
                .Button("Профиль", "profile")
                .Button("Помощь", "help")
                .Row()
                .Button("Настройки", "settings")
                .Button("Контакты", "share_contact")
                .Row()
                .Url("Документация", "https://core.telegram.org/bots/api")
                .Build();

            await ctx.ReplyAsync(
                "Добро пожаловать! Выберите действие:\n\n" +
                "Кнопки ниже — InlineKeyboard (прикреплены к сообщению).\n" +
                "При нажатии отправляют callbackData боту, кроме Url — она откроет ссылку.",
                keyboard);
        });
    }
}
