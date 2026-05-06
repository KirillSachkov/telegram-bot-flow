using System.Globalization;
using TelegramBotFlow.App.Features.Feedback.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.App.Features.Feedback.Wizards;

/// <summary>
///     Трёхшаговый визард сбора обратной связи.
///
///     Шаг 1 «rating»   — выбор оценки 1–5 через inline-кнопки (ButtonStep).
///     Шаг 2 «category» — выбор категории через inline-кнопки (ButtonStep, динамичный prompt).
///     Шаг 3 «comment»  — необязательный текстовый комментарий (TextStep с isOptional).
/// </summary>
public sealed class FeedbackWizard : BotWizard<FeedbackState>
{
    protected override void ConfigureSteps(WizardBuilder<FeedbackState> builder)
    {
        builder
            .ButtonStep(
                id: "rating",
                prompt: "💬 <b>Обратная связь</b> — шаг 1 из 3\n\nОцените работу бота:",
                buttons: [("⭐ 1", "1"), ("⭐ 2", "2"), ("⭐ 3", "3"), ("⭐ 4", "4"), ("⭐ 5", "5")],
                onSelected: static (_, state, value) =>
                {
                    state.Rating = int.Parse(value, CultureInfo.InvariantCulture);
                    return StepResult.GoTo("category");
                })
            .ButtonStep(
                id: "category",
                prompt: static (_, state) =>
                    $"💬 <b>Обратная связь</b> — шаг 2 из 3\n\n" +
                    $"Вы поставили: {new string('⭐', state.Rating)}\n\n" +
                    $"Выберите категорию:",
                buttons: [("🐛 Ошибка", "bug"), ("💡 Идея", "idea"), ("📝 Другое", "other")],
                onSelected: static (_, state, value) =>
                {
                    state.Category = value;
                    return StepResult.GoTo("comment");
                },
                columns: 1)
            .TextStep(
                id: "comment",
                prompt: "💬 <b>Обратная связь</b> — шаг 3 из 3\n\nНапишите комментарий:",
                isOptional: true,
                onInput: static (_, state, text) =>
                {
                    state.Comment = text;
                    return StepResult.Finish();
                });
    }

    public override Task<IEndpointResult> OnFinishedAsync(UpdateContext ctx, FeedbackState state)
    {
        // В реальном проекте здесь можно сохранить в БД или уведомить администратора.
        return Task.FromResult(BotResults.NavigateToRoot<FeedbackResultScreen>());
    }
}
