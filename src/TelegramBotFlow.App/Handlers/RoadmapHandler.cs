using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Data;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.App.Handlers;

/// <summary>
/// Обрабатывает кнопку получения roadmap и отправляет связанное сообщение.
/// </summary>
public sealed class RoadmapHandler : IBotEndpoint
{
    private const string ROADMAP_FALLBACK_TEXT =
        "<b>🗺 Roadmap</b>\n\n" +
        "<b>Q1 2025</b>\n" +
        "• Базовый pipeline и middleware\n" +
        "• Screen-навигация (Zero-Spam)\n" +
        "• InMemory и Redis сессии\n\n" +
        "<b>Q2 2025</b>\n" +
        "• Action-кнопки и ShowViewAsync\n" +
        "• Wizard / пошаговый ввод данных\n" +
        "• Broadcasts и рассылки\n\n" +
        "<b>Q3 2025</b>\n" +
        "• Loading state и Idempotency guard\n" +
        "• Webhook + Polling dual-mode\n" +
        "• Документация и примеры";

    /// <summary>
    /// Регистрирует callback-маршрут <c>get_roadmap</c>.
    /// </summary>
    /// <param name="app">Экземпляр приложения бота для регистрации маршрутов.</param>
    public void MapEndpoint(BotApplication app)
    {
        app.MapCallback("get_roadmap",
            async (UpdateContext ctx, BotDbContext db, ITelegramBotClient bot, IUpdateResponder responder) =>
            {
                await responder.AnswerCallbackAsync(ctx);

                BotSettings? settings = await db.Settings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == BotSettings.SINGLETON_ID, ctx.CancellationToken);

                if (settings?.Roadmap is { } roadmap)
                {
                    await bot.CopyMessage(
                        ctx.ChatId,
                        roadmap.SourceChatId,
                        roadmap.SourceMessageId,
                        cancellationToken: ctx.CancellationToken);
                    return;
                }

                await responder.ReplyAsync(ctx, ROADMAP_FALLBACK_TEXT);
            });
    }
}
