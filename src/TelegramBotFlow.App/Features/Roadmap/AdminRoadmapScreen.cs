using Microsoft.EntityFrameworkCore;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Data;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Features.Roadmap;

/// <summary>
/// Административный экран настройки источника roadmap-сообщения.
/// </summary>
public sealed class AdminRoadmapScreen : IScreen
{
    private readonly BotDbContext _db;

    /// <summary>
    /// Создаёт экран с доступом к данным настроек roadmap.
    /// </summary>
    /// <param name="db">Контекст данных бота.</param>
    public AdminRoadmapScreen(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Рендерит состояние roadmap и кнопки изменения/очистки привязки.
    /// </summary>
    /// <param name="ctx">Контекст текущего update-а.</param>
    /// <returns>Административный экран управления roadmap.</returns>
    public async Task<ScreenView> RenderAsync(UpdateContext ctx)
    {
        BotSettings? settings = await _db.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == BotSettings.SINGLETON_ID, ctx.CancellationToken);

        string status = settings?.Roadmap is null
            ? "⚠️ Не задан"
            : $"✅ Задан (message_id: {settings.Roadmap.SourceMessageId})";

        return new ScreenView($"<b>⚙️ Настройки Roadmap</b>\n\nСтатус: {status}")
            .NavigateButton<SetRoadmapInputScreen>("📥 Задать сообщение")
            .Row()
            .Button<ClearRoadmapAction>("🗑 Удалить")
            .BackButton();
    }
}
