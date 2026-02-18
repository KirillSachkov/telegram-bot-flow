using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.App.Handlers;

public sealed class FallbackHandler : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapFallback(async (UpdateContext ctx, IUpdateResponder responder) =>
        {
            await responder.ReplyAsync(ctx, "Не понимаю. Нажмите кнопку в меню или /start");
        });
    }
}
