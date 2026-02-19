using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Data;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.UI;
using static TelegramBotFlow.Core.Screens.NavCallbacks;

namespace TelegramBotFlow.App.Features.Roadmap;

/// <summary>Маркер действия получения Roadmap.</summary>
public struct GetRoadmapAction : IBotAction;

/// <summary>
/// Обрабатывает нажатие кнопки «Получить Roadmap»:
/// заменяет якорное сообщение скопированным roadmap-постом (или текстовым fallback),
/// прикрепляя кнопку возврата в главное меню.
/// </summary>
public sealed class GetRoadmapEndpoint : IBotEndpoint
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

    public void MapEndpoint(BotApplication app) =>
        app.MapAction<GetRoadmapAction>(Handle);

    private static async Task<IEndpointResult> Handle(
        UpdateContext ctx, BotDbContext db, IUpdateResponder responder)
    {
        BotSettings? settings = await db.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == BotSettings.SINGLETON_ID, ctx.CancellationToken);

        if (settings?.Roadmap is { } roadmap)
        {
            InlineKeyboardMarkup menuKeyboard = InlineKeyboard.SingleButton("☰ Главное меню", MENU);
            await responder.ReplaceAnchorWithCopyAsync(
                ctx, roadmap.SourceChatId, roadmap.SourceMessageId, menuKeyboard);
            return BotResults.Empty();
        }

        return BotResults.ShowView(new ScreenView(ROADMAP_FALLBACK_TEXT).MenuButton());
    }
}
