using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.App.Endpoints.Contact;

/// <summary>
/// MapUpdate — обработка полученного контакта (не текстовое сообщение).
/// </summary>
public sealed class ReceiveContact : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapUpdate(ctx => ctx.Update.Message?.Contact is not null, async ctx =>
        {
            var contact = ctx.Update.Message!.Contact!;

            ctx.ClearScreen();

            await ctx.ReplyAsync(
                $"Получен контакт:\n" +
                $"Имя: {contact.FirstName} {contact.LastName}\n" +
                $"Телефон: {contact.PhoneNumber}",
                ReplyKeyboard.Remove());
        });
    }
}
