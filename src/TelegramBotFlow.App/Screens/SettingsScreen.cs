using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Screens;

/// <summary>
/// Экран пользовательских настроек.
/// </summary>
public sealed class SettingsScreen : IScreen
{
    /// <summary>
    /// Возвращает временное представление раздела настроек.
    /// </summary>
    /// <param name="ctx">Контекст текущего update-а.</param>
    /// <returns>Экран настроек с кнопкой возврата.</returns>
    public Task<ScreenView> RenderAsync(UpdateContext ctx)
    {
        ScreenView view = new ScreenView(
                "<b>Настройки</b>\n\n" +
                "Раздел в разработке.")
            .BackButton();

        return Task.FromResult(view);
    }
}
