using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.App.Endpoints.Profile;

/// <summary>
/// Callback "profile" — показывает профиль с подменю.
/// Wildcard "profile:*" — обработка подменю (profile:edit, profile:stats).
/// </summary>
public sealed class ShowProfile : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapCallback("profile", async ctx =>
        {
            await ctx.AnswerCallbackAsync();

            var keyboard = new InlineKeyboard()
                .Button("Редактировать", "profile:edit")
                .Button("Статистика", "profile:stats")
                .Row()
                .Button("Назад в меню", "back_to_menu")
                .Build();

            await ctx.ReplyAsync(
                $"Ваш профиль:\n" +
                $"ID: {ctx.UserId}\n" +
                $"Чат: {ctx.ChatId}",
                keyboard);
        });

        app.MapCallbackGroup("profile", async (ctx, action) =>
        {
            await ctx.AnswerCallbackAsync();

            var text = action switch
            {
                "edit" => "Функция редактирования профиля.\n(Здесь будет вызов backend API)",
                "stats" => $"Статистика:\nСообщений: 42\nДней в боте: 7",
                _ => $"Неизвестное действие: {action}"
            };

            await ctx.ReplyAsync(text, InlineKeyboard.SingleButton("Назад к профилю", "profile"));
        });
    }
}
