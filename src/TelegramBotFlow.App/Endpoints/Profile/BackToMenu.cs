using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.App.Endpoints.Profile;

/// <summary>
/// Callback "back_to_menu" — редактирует сообщение, возвращая главное меню.
/// </summary>
public sealed class BackToMenu : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapCallback("back_to_menu", async ctx =>
        {
            await ctx.AnswerCallbackAsync();

            var keyboard = new InlineKeyboard()
                .Button("Профиль", "profile")
                .Button("Помощь", "help")
                .Row()
                .Button("Настройки", "settings")
                .Button("Контакты", "share_contact")
                .Row()
                .Url("Документация", "https://core.telegram.org/bots/api")
                .Build();

            await ctx.EditMessageAsync("Добро пожаловать! Выберите действие:", keyboard);
        });
    }
}
