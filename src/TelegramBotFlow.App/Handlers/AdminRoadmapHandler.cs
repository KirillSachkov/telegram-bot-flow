using TelegramBotFlow.App.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Data;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;

namespace TelegramBotFlow.App.Handlers;

/// <summary>
/// Административные маршруты для управления привязкой roadmap-сообщения.
/// </summary>
public sealed class AdminRoadmapHandler : IBotEndpoint
{
    /// <summary>
    /// Регистрирует admin callback/input маршруты для настройки roadmap.
    /// </summary>
    /// <param name="app">Экземпляр приложения бота для регистрации маршрутов.</param>
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<ClearRoadmapAction>(
            async (UpdateContext ctx, BotDbContext db) =>
            {
                if (!ctx.IsAdmin)
                    return BotResults.Stay();

                await ClearRoadmapAsync(ctx, db);
                return BotResults.Refresh("✅ Привязка удалена");
            });

        app.MapInput<SetRoadmapInput>(async (UpdateContext ctx, BotDbContext db) =>
        {
            BotSettings settings = await db.Settings.FindAsync(BotSettings.SINGLETON_ID)
                                   ?? db.Settings.Add(new BotSettings()).Entity;

            settings.Roadmap = new RoadmapMessageConfig
            {
                SourceChatId = ctx.ChatId, SourceMessageId = ctx.MessageId!.Value
            };

            await db.SaveChangesAsync(ctx.CancellationToken);

            return BotResults.Back("✅ Roadmap успешно сохранён");
        });
    }

    /// <summary>
    /// Очищает сохранённую привязку roadmap, если она существует.
    /// </summary>
    /// <param name="ctx">Текущий контекст update-а.</param>
    /// <param name="db">Контекст данных бота.</param>
    private static async Task ClearRoadmapAsync(UpdateContext ctx, BotDbContext db)
    {
        BotSettings? settings = await db.Settings.FindAsync(BotSettings.SINGLETON_ID);
        if (settings is null || settings.Roadmap is null)
            return;

        settings.Roadmap = null;
        await db.SaveChangesAsync(ctx.CancellationToken);
    }
}
