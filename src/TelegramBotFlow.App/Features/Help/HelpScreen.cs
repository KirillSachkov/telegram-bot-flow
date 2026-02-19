using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Features.Help;

/// <summary>
/// Экран справки с базовыми командами и ссылкой на Telegram Bot API.
/// </summary>
public sealed class HelpScreen : IScreen
{
    /// <summary>
    /// Рендерит экран справки и возвращает пользователя к предыдущему экрану.
    /// </summary>
    /// <param name="ctx">Контекст текущего update-а.</param>
    /// <returns>Экран справки.</returns>
    public Task<ScreenView> RenderAsync(UpdateContext ctx)
    {
        ScreenView view = new ScreenView(
                "<b>Справка</b>\n\n" +
                "/start — главное меню\n" +
                "/help — эта справка\n\n" +
                "Используйте кнопки для навигации.")
            .UrlButton("Telegram Bot API", "https://core.telegram.org/bots/api")
            .Row()
            .BackButton();

        return Task.FromResult(view);
    }
}
