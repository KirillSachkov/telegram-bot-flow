using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Data;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;

namespace TelegramBotFlow.App.Features.Roadmap;

/// <summary>Маркер действия очистки Roadmap-привязки.</summary>
public struct ClearRoadmapAction : IBotAction;

/// <summary>
/// Обрабатывает нажатие кнопки «Удалить» в административном экране Roadmap:
/// очищает сохранённую привязку сообщения.
/// </summary>
public sealed class ClearRoadmapEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app) =>
        app.MapAction<ClearRoadmapAction>(Handle);

    private static async Task<IEndpointResult> Handle(UpdateContext ctx, BotDbContext db)
    {
        if (!ctx.IsAdmin)
            return BotResults.Stay();

        BotSettings? settings = await db.Settings.FindAsync(BotSettings.SINGLETON_ID);
        if (settings is { Roadmap: not null })
        {
            settings.Roadmap = null;
            _ = await db.SaveChangesAsync(ctx.CancellationToken);
        }

        return BotResults.Refresh("✅ Привязка удалена");
    }
}
