using TelegramBotFlow.App.Features.Feedback.Endpoints;
using TelegramBotFlow.App.Features.Help.Screens;
using TelegramBotFlow.App.Features.Onboarding.Endpoints;
using TelegramBotFlow.App.Features.Profile.Screens;
using TelegramBotFlow.App.Features.Settings.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Features.MainMenu.Screens;

/// <summary>
/// Main menu screen with navigation to key sections.
/// </summary>
public sealed class MainMenuScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        ScreenView view = new ScreenView("Добро пожаловать! Выберите раздел:")
            .NavigateButton<ProfileScreen>("Профиль")
            .NavigateButton<SettingsScreen>("Настройки")
            .Row()
            .NavigateButton<HelpScreen>("Помощь")
            .Row()
            .Button<StartProfileSetupAction>("✏️ Настроить профиль")
            .Button<StartFeedbackAction>("💬 Оставить отзыв");

        return ValueTask.FromResult(view);
    }
}
