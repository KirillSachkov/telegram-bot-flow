using TelegramBotFlow.App.Features.Onboarding.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.App.Features.Onboarding.Wizards;

/// <summary>
///     Двухшаговый визард настройки профиля.
///     Шаг 1 «name» — имя пользователя (TextStep).
///     Шаг 2 «age»  — возраст (TextStep, валидация числа 1–120).
/// </summary>
public sealed class ProfileSetupWizard : BotWizard<ProfileSetupState>
{
    protected override void ConfigureSteps(WizardBuilder<ProfileSetupState> builder)
    {
        builder
            .TextStep(
                id: "name",
                prompt: "👤 <b>Настройка профиля</b> — шаг 1 из 2\n\nВведите ваше имя:",
                onInput: static (_, state, text) =>
                {
                    if (text.Length > 50)
                        return StepResult.Stay("Имя слишком длинное (максимум 50 символов).");

                    state.Name = text;
                    return StepResult.GoTo("age");
                })
            .TextStep(
                id: "age",
                prompt: static (_, state) =>
                    $"👤 <b>Настройка профиля</b> — шаг 2 из 2\n\n" +
                    $"Привет, <b>{state.Name}</b>!\n\n" +
                    $"Введите ваш возраст (число от 1 до 120):",
                onInput: static (_, state, text) =>
                {
                    if (!int.TryParse(text, out int age) || age is < 1 or > 120)
                        return StepResult.Stay("Укажите корректный возраст (число от 1 до 120).");

                    state.Age = age;
                    return StepResult.Finish();
                });
    }

    public override Task<IEndpointResult> OnFinishedAsync(UpdateContext ctx, ProfileSetupState state)
    {
        ctx.Session?.Data.Set("name", state.Name);
        ctx.Session?.Data.Set("age", state.Age.ToString());

        return Task.FromResult(BotResults.NavigateToRoot<ProfileSetupResultScreen>());
    }
}
