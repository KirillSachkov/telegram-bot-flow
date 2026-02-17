using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.App.Endpoints.Settings;

/// <summary>
/// Кнопка "О боте" на экране SettingsMain.
/// </summary>
public sealed class AboutBot : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapButton(Screens.SettingsMain, "О боте", async ctx =>
        {
            await ctx.ReplyAsync(
                "Telegram Bot Flow — template для создания ботов.\n" +
                "Версия: 1.0.0");
        });
    }
}
