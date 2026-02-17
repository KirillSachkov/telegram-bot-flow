using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.App.Endpoints.Contact;

/// <summary>
/// Кнопка "Отмена" на экране ContactShare — отменяет действие и убирает ReplyKeyboard.
/// </summary>
public sealed class CancelContact : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapButton(Screens.ContactShare, "Отмена", async ctx =>
        {
            ctx.ClearScreen();

            await ctx.ReplyAsync("Действие отменено.", ReplyKeyboard.Remove());
        });
    }
}
