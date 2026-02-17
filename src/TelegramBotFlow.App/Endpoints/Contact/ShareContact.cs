using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.App.Endpoints.Contact;

/// <summary>
/// Callback "share_contact" — показывает ReplyKeyboard с RequestContact и RequestLocation.
/// </summary>
public sealed class ShareContact : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapCallback("share_contact", async ctx =>
        {
            await ctx.AnswerCallbackAsync();

            var keyboard = new ReplyKeyboard()
                .RequestContact("Отправить номер телефона")
                .Row()
                .RequestLocation("Отправить геолокацию")
                .Row()
                .Button("Отмена")
                .OneTime()
                .Build();

            await ctx.ReplyWithScreenAsync(Screens.ContactShare,
                "Специальные кнопки ReplyKeyboard:\n\n" +
                "RequestContact — запрашивает номер телефона.\n" +
                "RequestLocation — запрашивает геолокацию.\n\n" +
                "Нажмите кнопку ниже:",
                keyboard);
        });
    }
}
