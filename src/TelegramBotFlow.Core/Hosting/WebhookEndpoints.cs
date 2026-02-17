using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;

namespace TelegramBotFlow.Core.Hosting;

public static class WebhookEndpoints
{
    public static async Task HandleWebhookUpdate(
        Update update,
        ITelegramBotClient bot,
        UpdatePipeline pipeline,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var context = new UpdateContext(update, bot, services, cancellationToken);
        await pipeline.ProcessAsync(context);
    }
}
