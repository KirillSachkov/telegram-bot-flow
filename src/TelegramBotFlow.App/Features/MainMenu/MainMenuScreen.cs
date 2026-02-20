using TelegramBotFlow.App.Features.Feedback;
using TelegramBotFlow.App.Features.Help;
using TelegramBotFlow.App.Features.Onboarding;
using TelegramBotFlow.App.Features.Profile;
using TelegramBotFlow.App.Features.Roadmap;
using TelegramBotFlow.App.Features.Settings;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Features.MainMenu;

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
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        ScreenView view = new ScreenView("Добро пожаловать! Выберите раздел:")
            .NavigateButton<ProfileScreen>("Профиль")
            .NavigateButton<SettingsScreen>("Настройки")
            .Row()
            .NavigateButton<HelpScreen>("Помощь")
            .Row()
            .Button<GetRoadmapAction>("🗺 Получить Roadmap")
            .Row()
            .Button<StartProfileSetupAction>("✏️ Настроить профиль")
            .Button<StartFeedbackAction>("💬 Оставить отзыв");

        if (ctx.IsAdmin)
            _ = view.Row().NavigateButton<AdminRoadmapScreen>("⚙️ Настройки Roadmap");

        return ValueTask.FromResult(view);
    }
}
