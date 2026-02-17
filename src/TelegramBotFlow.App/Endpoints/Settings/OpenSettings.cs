using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.App.Endpoints.Settings;

/// <summary>
/// /settings — показывает ReplyKeyboard с настройками.
/// Callback "settings" — подсказка из InlineKeyboard в /start.
/// </summary>
public sealed class OpenSettings : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapCommand("/settings", async ctx =>
        {
            var keyboard = Shared.BuildSettingsKeyboard();

            await ctx.ReplyWithScreenAsync(Screens.SettingsMain,
                "Настройки:\n\n" +
                "Кнопки ниже — ReplyKeyboard.\n" +
                "Они работают только когда вы на этом экране.",
                keyboard);
        });

        app.MapCallback("settings", async ctx =>
        {
            await ctx.AnswerCallbackAsync();
            await ctx.ReplyAsync("Введите /settings для открытия настроек.");
        });
    }
}
