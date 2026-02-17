using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Flows;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.App.Endpoints.Registration;

/// <summary>
/// /register — flow-анкета регистрации с 5 шагами.
/// Callback "register" — подсказка из InlineKeyboard.
/// </summary>
public sealed class RegisterFlow : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapCallback("register", async ctx =>
        {
            await ctx.AnswerCallbackAsync();
            await ctx.ReplyAsync("Начнём регистрацию! Введите /register");
        });

        app.MapFlow("/register", flow => flow
            .Ask("name", "Как вас зовут?",
                validate: Validators.Combine(Validators.NotEmpty, Validators.MaxLength(50)))

            .Ask("email", "Введите email:",
                validate: Validators.Email)

            .Ask("age", "Сколько вам лет?",
                validate: Validators.Integer)

            .Ask("gender", "Ваш пол:",
                validate: Validators.NotEmpty,
                replyMarkup: new ReplyKeyboard()
                    .Button("Мужской")
                    .Button("Женский")
                    .OneTime()
                    .Build())

            .Ask("city", "Ваш город (выберите или введите вручную):",
                validate: Validators.NotEmpty,
                replyMarkup: new ReplyKeyboard()
                    .Button("Москва")
                    .Button("Санкт-Петербург")
                    .Row()
                    .Button("Казань")
                    .Button("Другой")
                    .OneTime()
                    .Build())

            .OnComplete(async (ctx, answers) =>
            {
                var summary = "Регистрация завершена!\n\n" +
                              $"Имя: {answers["name"]}\n" +
                              $"Email: {answers["email"]}\n" +
                              $"Возраст: {answers["age"]}\n" +
                              $"Пол: {answers["gender"]}\n" +
                              $"Город: {answers["city"]}";

                await ctx.ReplyAsync(summary, ReplyKeyboard.Remove());
            }));
    }
}
