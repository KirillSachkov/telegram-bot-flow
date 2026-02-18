using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Screens;

public sealed class HelpScreen : IScreen
{
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
