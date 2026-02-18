using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Screens;

public sealed class MainMenuScreen : IScreen
{
    public Task<ScreenView> RenderAsync(UpdateContext ctx) =>
        Task.FromResult(new ScreenView(
                "Добро пожаловать! Выберите раздел:")
            .NavigateButton<ProfileScreen>("Профиль")
            .NavigateButton<SettingsScreen>("Настройки")
            .Row()
            .NavigateButton<HelpScreen>("Помощь")
            .Row()
            .Button("🗺 Получить Roadmap", "get_roadmap"));
}
