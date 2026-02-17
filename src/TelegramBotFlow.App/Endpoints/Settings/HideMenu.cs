using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.App.Endpoints.Settings;

/// <summary>
/// Кнопка "Скрыть меню" на экране SettingsMain — убирает ReplyKeyboard и сбрасывает экран.
/// </summary>
public sealed class HideMenu : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapButton(Screens.SettingsMain, "Скрыть меню", async ctx =>
        {
            ctx.ClearScreen();

            await ctx.ReplyAsync(
                "Меню скрыто. Используйте /settings чтобы вернуть.",
                ReplyKeyboard.Remove());
        });
    }
}
