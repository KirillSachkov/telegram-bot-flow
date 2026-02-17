using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.App.Endpoints.Settings;

/// <summary>
/// Кнопка "Уведомления" на экране SettingsMain → InlineKeyboard.
/// Wildcard callback "notify:*" для notify:on / notify:off.
/// </summary>
public sealed class ToggleNotifications : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapButton(Screens.SettingsMain, "Уведомления", async ctx =>
        {
            var keyboard = new InlineKeyboard()
                .Button("Включить", "notify:on")
                .Button("Выключить", "notify:off")
                .Build();

            await ctx.ReplyAsync("Настройка уведомлений:", keyboard);
        });

        app.MapCallbackGroup("notify", async (ctx, action) =>
        {
            await ctx.AnswerCallbackAsync();
            var state = action == "on" ? "включены" : "выключены";
            await ctx.ReplyAsync($"Уведомления {state}.");
        });
    }
}
