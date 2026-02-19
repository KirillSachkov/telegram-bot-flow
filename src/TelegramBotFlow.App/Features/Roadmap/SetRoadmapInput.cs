using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Data;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;

namespace TelegramBotFlow.App.Features.Roadmap;

/// <summary>Маркер действия ввода Roadmap-сообщения.</summary>
public struct SetRoadmapInput : IBotAction;

/// <summary>
/// Обрабатывает пользовательский ввод после перехода к <see cref="SetRoadmapInputScreen"/>:
/// копирует сообщение в Теневой канал (Storage Channel) и сохраняет его координаты как источник Roadmap.
/// Если <see cref="BotConfiguration.StorageChannelId"/> не задан (равен 0), сохраняет исходные координаты
/// без копирования (обратная совместимость).
/// </summary>
public sealed class SetRoadmapInputEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app) =>
        app.MapInput<SetRoadmapInput>(Handle);

    private static async Task<IEndpointResult> Handle(
        UpdateContext ctx,
        BotDbContext db,
        ITelegramBotClient bot,
        IOptions<BotConfiguration> botOptions)
    {
        BotSettings settings = await db.Settings.FindAsync(BotSettings.SINGLETON_ID)
                               ?? db.Settings.Add(new BotSettings()).Entity;

        long storageChannelId = botOptions.Value.StorageChannelId;

        if (storageChannelId != 0)
        {
            MessageId copied = await bot.CopyMessage(
                storageChannelId,
                ctx.ChatId,
                ctx.MessageId!.Value,
                cancellationToken: ctx.CancellationToken);

            settings.Roadmap = new RoadmapMessageConfig
            {
                SourceChatId = storageChannelId,
                SourceMessageId = copied.Id
            };

            await db.SaveChangesAsync(ctx.CancellationToken);

            await bot.DeleteMessage(ctx.ChatId, ctx.MessageId!.Value, ctx.CancellationToken);
        }
        else
        {
            settings.Roadmap = new RoadmapMessageConfig
            {
                SourceChatId = ctx.ChatId,
                SourceMessageId = ctx.MessageId!.Value
            };

            await db.SaveChangesAsync(ctx.CancellationToken);
        }

        return BotResults.Back("✅ Roadmap успешно сохранён");
    }
}
