using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Screens;

public sealed class SettingsScreen : IScreen
{
    public Task<ScreenView> RenderAsync(UpdateContext ctx)
    {
        ScreenView view = new ScreenView(
                "<b>Настройки</b>\n\n" +
                "Раздел в разработке.")
            .BackButton();

        return Task.FromResult(view);
    }
}
