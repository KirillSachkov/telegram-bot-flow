using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Screens;

/// <summary>
/// Главный экран приложения с переходами в ключевые разделы.
/// </summary>
public sealed class MainMenuScreen : IScreen
{
    /// <summary>
    /// Формирует представление главного меню с набором действий по роли пользователя.
    /// </summary>
    /// <param name="ctx">Контекст текущего update-а.</param>
    /// <returns>Экран с кнопками разделов и действия roadmap.</returns>
    public Task<ScreenView> RenderAsync(UpdateContext ctx)
    {
        ScreenView view = new ScreenView("Добро пожаловать! Выберите раздел:")
            .NavigateButton<ProfileScreen>("Профиль")
            .NavigateButton<SettingsScreen>("Настройки")
            .Row()
            .NavigateButton<HelpScreen>("Помощь")
            .Row();

        if (ctx.IsAdmin)
            view.NavigateButton<AdminRoadmapScreen>("⚙️ Настройки Roadmap");
        else
            view.Button("🗺 Получить Roadmap", "get_roadmap");

        return Task.FromResult(view);
    }
}
