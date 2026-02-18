using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Handlers;

public sealed class RoadmapHandler : IBotEndpoint
{
    private const string ROADMAP_TEXT =
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

    public void MapEndpoint(BotApplication app)
    {
        app.MapAction("get_roadmap", () =>
            Task.FromResult(new ScreenView(ROADMAP_TEXT).MenuButton()));
    }
}
