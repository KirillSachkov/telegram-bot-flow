using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.Core.Hosting;

internal sealed class BotRuntime
{
    private readonly WebApplication _app;
    private readonly IServiceProvider _services;

    public BotRuntime(WebApplication app)
    {
        _app = app;
        _services = app.Services;
    }

    public async Task RunAsync(UpdatePipeline pipeline, MenuBuilder? menuBuilder)
    {
        ReplaceUpdatePipeline(_services, pipeline);

        BotConfiguration config = _services.GetRequiredService<IOptions<BotConfiguration>>().Value;

        if (config.Mode == BotMode.WEBHOOK)
            await ConfigureWebhookAsync(config, pipeline);

        if (menuBuilder is not null)
            await ApplyMenuAsync(menuBuilder);

        await _app.RunAsync();
    }

    private async Task ConfigureWebhookAsync(BotConfiguration config, UpdatePipeline pipeline)
    {
        _app.MapPost(config.WebhookPath, async (
            Update update,
            IServiceProvider sp,
            CancellationToken ct) =>
        {
            await WebhookEndpoints.HandleWebhookUpdate(update, pipeline, sp, ct);
            return Results.Ok();
        });

        ITelegramBotClient bot = _services.GetRequiredService<ITelegramBotClient>();
        await bot.SetWebhook(config.WebhookUrl + config.WebhookPath, allowedUpdates: []);
    }

    private async Task ApplyMenuAsync(MenuBuilder menuBuilder)
    {
        ITelegramBotClient bot = _services.GetRequiredService<ITelegramBotClient>();
        await menuBuilder.ApplyAsync(bot);
    }

    private static void ReplaceUpdatePipeline(IServiceProvider services, UpdatePipeline pipeline)
    {
        PipelineHolder holder = services.GetRequiredService<PipelineHolder>();
        holder.Pipeline = pipeline;
    }
}