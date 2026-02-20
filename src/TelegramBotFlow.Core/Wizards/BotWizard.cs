using System.Text.Json;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Wizards;

/// <summary>
/// DTO для ответа от Визарда к Middleware, определяющий дальнейшее поведение.
/// </summary>
public sealed record WizardTransition(
    bool IsFinished,
    IEndpointResult? EndpointResult = null);

/// <summary>
/// Общий контракт для всех визардов, независимый от типа состояния.
/// Используется Middleware для обработки апдейтов без использования рефлексии.
/// </summary>
public interface IBotWizard
{
    /// <summary>
    /// Принимает сырой стейт из базы, обрабатывает апдейт и возвращает команду для Middleware.
    /// Состояние <paramref name="storageState"/> может быть изменено внутри метода.
    /// </summary>
    Task<WizardTransition> ProcessUpdateAsync(UpdateContext context, WizardStorageState storageState);

    /// <summary>
    /// Инициализирует и запускает первый шаг визарда.
    /// </summary>
    Task<WizardTransition> InitializeAsync(UpdateContext context, WizardStorageState storageState);
}

/// <summary>
/// Базовый класс для всех визардов (машин состояний).
/// Управляет жизненным циклом (рендеринг, процессинг, роутинг) шагов.
/// </summary>
/// <typeparam name="TState">Тип состояния (DTO для сбора данных).</typeparam>
public abstract class BotWizard<TState> : IBotWizard where TState : class, new()
{
    private IReadOnlyDictionary<string, WizardStep<TState>>? _steps;
    private string? _initialStepId;

    /// <summary>
    /// Конфигурация шагов (Builder pattern).
    /// </summary>
    protected abstract void ConfigureSteps(WizardBuilder<TState> builder);

    /// <summary>
    /// Вызывается после успешного завершения всех шагов визарда.
    /// Должен вернуть результат для роутинга (например, <see cref="NavigateToRootResult"/>).
    /// </summary>
    /// <param name="context">Текущий Update-контекст.</param>
    /// <param name="state">Итоговое заполненное состояние визарда.</param>
    public abstract Task<IEndpointResult> OnFinishedAsync(UpdateContext context, TState state);

    /// <summary>
    /// Время жизни состояния визарда (опционально).
    /// По умолчанию <see langword="null"/> (никогда не истекает).
    /// </summary>
    protected virtual TimeSpan? ExpiresAfter => null;

    internal WizardStep<TState> GetStep(string stepId)
    {
        EnsureConfigured();

        if (_steps!.TryGetValue(stepId, out WizardStep<TState>? step))
            return step;

        throw new InvalidOperationException($"Step with ID '{stepId}' not found in wizard {GetType().Name}");
    }

    internal WizardStep<TState> GetInitialStep()
    {
        EnsureConfigured();
        return _steps![_initialStepId!];
    }

    internal DateTime? CalculateExpiration() =>
        ExpiresAfter.HasValue ? DateTime.UtcNow.Add(ExpiresAfter.Value) : null;

    public async Task<WizardTransition> InitializeAsync(UpdateContext context, WizardStorageState storageState)
    {
        EnsureConfigured();

        WizardStep<TState> initialStep = _steps![_initialStepId!];
        storageState.CurrentStepId = initialStep.Id;
        storageState.ExpiresAt = CalculateExpiration();

        TState state = new();

        if (initialStep.OnEnter is not null)
        {
            await initialStep.OnEnter(context, state);
        }

        ScreenView view = await initialStep.Renderer(context, state);

        storageState.PayloadJson = JsonSerializer.Serialize(state);

        return new WizardTransition(false, BotResults.ShowView(view));
    }

    public async Task<WizardTransition> ProcessUpdateAsync(UpdateContext context, WizardStorageState storageState)
    {
        EnsureConfigured();

        TState state = string.IsNullOrWhiteSpace(storageState.PayloadJson)
            ? new TState()
            : JsonSerializer.Deserialize<TState>(storageState.PayloadJson)!;

        if (!_steps!.TryGetValue(storageState.CurrentStepId, out WizardStep<TState>? currentStep))
            throw new InvalidOperationException($"Step {storageState.CurrentStepId} not found");

        StepResult result = await currentStep.Processor(context, state);

        if (result is StepResult.GoToResult goTo)
        {
            storageState.CurrentStepId = goTo.StepId;
            storageState.PayloadJson = JsonSerializer.Serialize(state);

            WizardStep<TState> nextStep = _steps[goTo.StepId];

            if (nextStep.OnEnter is not null)
            {
                await nextStep.OnEnter(context, state);
                storageState.PayloadJson = JsonSerializer.Serialize(state);
            }

            ScreenView view = await nextStep.Renderer(context, state);
            return new WizardTransition(false, BotResults.ShowView(view));
        }

        if (result is StepResult.FinishResult)
        {
            IEndpointResult finalResult = await OnFinishedAsync(context, state);
            return new WizardTransition(true, finalResult);
        }

        if (result is StepResult.StayResult stay)
        {
            storageState.PayloadJson = JsonSerializer.Serialize(state);

            IEndpointResult endpointResult = string.IsNullOrWhiteSpace(stay.Notification)
                ? BotResults.Empty()
                : BotResults.Stay(stay.Notification);

            return new WizardTransition(false, endpointResult);
        }

        throw new InvalidOperationException("Unknown step result");
    }

    private void EnsureConfigured()
    {
        if (_steps is not null)
            return;

        var builder = new WizardBuilder<TState>();
        ConfigureSteps(builder);
        _steps = builder.Build(out _initialStepId);
    }
}
