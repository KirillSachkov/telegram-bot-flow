using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Features.Settings;

/// <summary>
/// Экран пользовательских настроек.
/// </summary>
public sealed class SettingsScreen : IScreen
{
    /// <inheritdoc/>
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
        => ValueTask.FromResult(
            new ScreenView(
                    "<b>Настройки</b>\n\n" +
                    "Раздел в разработке.")
                .BackButton());
}
