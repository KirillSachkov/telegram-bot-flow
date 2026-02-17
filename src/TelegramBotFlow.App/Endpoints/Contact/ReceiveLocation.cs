using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.App.Endpoints.Contact;

/// <summary>
/// MapUpdate — обработка полученной геолокации.
/// </summary>
public sealed class ReceiveLocation : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapUpdate(ctx => ctx.Update.Message?.Location is not null, async ctx =>
        {
            var location = ctx.Update.Message!.Location!;

            ctx.ClearScreen();

            await ctx.ReplyAsync(
                $"Получена геолокация:\n" +
                $"Широта: {location.Latitude}\n" +
                $"Долгота: {location.Longitude}",
                ReplyKeyboard.Remove());
        });
    }
}
