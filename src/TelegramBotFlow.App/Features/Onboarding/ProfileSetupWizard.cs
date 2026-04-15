using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.App.Features.Onboarding;

// ── Action-маркер для запуска визарда ──────────────────────────────────────
public struct StartProfileSetupAction : IBotAction;

// ── State ──────────────────────────────────────────────────────────────────
public class ProfileSetupState
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

// ── Endpoint (кнопка-триггер) ──────────────────────────────────────────────
/// <summary>
/// Регистрирует кнопку-триггер запуска мастера настройки профиля.
/// </summary>
public sealed class ProfileSetupEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app) =>
        app.MapAction<StartProfileSetupAction>((UpdateContext _) => BotResults.StartWizard<ProfileSetupWizard>());
}

// ── Wizard ─────────────────────────────────────────────────────────────────
/// <summary>
/// Двухшаговый визард настройки профиля.
/// Шаг 1 «name» — имя пользователя (TextStep).
/// Шаг 2 «age»  — возраст (TextStep, валидация числа 1–120).
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

// ── Экран результата ────────────────────────────────────────────────────────
/// <summary>
/// Экран подтверждения после завершения <see cref="ProfileSetupWizard"/>.
/// </summary>
public sealed class ProfileSetupResultScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        string name = ctx.Session?.Data.GetString("name") ?? "—";
        string age = ctx.Session?.Data.GetString("age") ?? "—";

        return ValueTask.FromResult(new ScreenView(
                $"✅ <b>Профиль сохранён!</b>\n\n" +
                $"Имя: <b>{name}</b>\n" +
                $"Возраст: <b>{age}</b>")
            .MenuButton());
    }
}
