using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.App.Endpoints.Settings;

/// <summary>
/// Кнопка "Язык" на экране SettingsMain → подэкран SettingsLang.
/// Кнопки "Русский", "English", "Назад" на экране SettingsLang.
/// </summary>
public sealed class ChangeLanguage : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapButton(Screens.SettingsMain, "Язык", async ctx =>
        {
            var keyboard = new ReplyKeyboard()
                .Button("Русский")
                .Button("English")
                .Row()
                .Button("Назад")
                .OneTime()
                .Build();

            await ctx.ReplyWithScreenAsync(Screens.SettingsLang, "Выберите язык:", keyboard);
        });

        app.MapButton(Screens.SettingsLang, "Русский", HandleLanguageChange);
        app.MapButton(Screens.SettingsLang, "English", HandleLanguageChange);

        app.MapButton(Screens.SettingsLang, "Назад", async ctx =>
        {
            await ctx.ReplyWithScreenAsync(Screens.SettingsMain, "Настройки:", Shared.BuildSettingsKeyboard());
        });
    }

    private static async Task HandleLanguageChange(UpdateContext ctx)
    {
        await ctx.ReplyWithScreenAsync(Screens.SettingsMain,
            $"Язык изменён на: {ctx.MessageText}", Shared.BuildSettingsKeyboard());
    }
}
