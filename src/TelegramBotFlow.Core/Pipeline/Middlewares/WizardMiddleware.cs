using Microsoft.Extensions.Logging;
using TelegramBotFlow.Core.Constants;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

/// <summary>
/// Middleware перехвата апдейтов, если пользователь находится в активном визарде.
/// Должен быть зарегистрирован после SessionMiddleware, но до роутера.
///
/// Middleware занимается только оркестрацией — не принимает UI-решений самостоятельно.
/// Решение об удалении сообщения делегируется визарду через <see cref="WizardTransition.ShouldDeleteUserMessage"/>.
/// </summary>
internal sealed class WizardMiddleware : IUpdateMiddleware
{
    private readonly IWizardStore _wizardStore;
    private readonly WizardRegistry _wizardRegistry;
    private readonly ILogger<WizardMiddleware> _logger;

    public WizardMiddleware(
        IWizardStore wizardStore,
        WizardRegistry wizardRegistry,
        ILogger<WizardMiddleware> logger)
    {
        _wizardStore = wizardStore;
        _wizardRegistry = wizardRegistry;
        _logger = logger;
    }

    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        if (context.Session is null)
        {
            await next(context);
            return;
        }

        string? activeWizardId = context.Session.Navigation.ActiveWizardId;

        if (string.IsNullOrWhiteSpace(activeWizardId))
        {
            await next(context);
            return;
        }

        if (context.MessageText is BotCommands.CANCEL or BotCommands.START)
        {
            _logger.LogInformation("Wizard {WizardId} cancelled by user via command", activeWizardId);
            context.Session.Navigation.ActiveWizardId = null;
            await _wizardStore.DeleteAsync(context.UserId, activeWizardId, context.CancellationToken);
            await next(context);
            return;
        }

        // nav:menu и nav:close — всегда выходят из визарда и передают управление роутеру.
        // Кнопка "Главное меню" и "Закрыть" должны работать внутри визарда.
        if (context.CallbackData is NavCallbacks.MENU or NavCallbacks.CLOSE)
        {
            _logger.LogInformation("Wizard {WizardId} cancelled by user via nav callback {Callback}",
                activeWizardId, context.CallbackData);
            context.Session.Navigation.ActiveWizardId = null;
            await _wizardStore.DeleteAsync(context.UserId, activeWizardId, context.CancellationToken);
            await next(context);
            return;
        }

        WizardStorageState? storageState =
            await _wizardStore.GetAsync(context.UserId, activeWizardId, context.CancellationToken);

        if (storageState is null)
        {
            _logger.LogWarning("Wizard {WizardId} state not found or expired", activeWizardId);
            context.Session.Navigation.ActiveWizardId = null;
            await next(context);
            return;
        }

        if (!_wizardRegistry.HasWizard(activeWizardId))
        {
            _logger.LogError("Wizard implementation {WizardId} not found in registry", activeWizardId);
            context.Session.Navigation.ActiveWizardId = null;
            await _wizardStore.DeleteAsync(context.UserId, activeWizardId, context.CancellationToken);
            await next(context);
            return;
        }

        IBotWizard wizardInstance = _wizardRegistry.Resolve(activeWizardId, context.RequestServices);
        BotExecutionContext botContext = BotExecutionContext.FromUpdateContext(context);

        try
        {
            // nav:back — GoBack внутри визарда (возврат на предыдущий шаг).
            // Если шагов в истории нет, GoBackAsync вернёт IsFinished=true и выполнит Back().
            bool isNavBack = context.CallbackData == NavCallbacks.BACK;

            WizardTransition transition = isNavBack
                ? await wizardInstance.GoBackAsync(context, storageState)
                : await wizardInstance.ProcessUpdateAsync(context, storageState);

            // Решение об удалении сообщения принято самим визардом — middleware не инспектирует тип результата
            if (transition.ShouldDeleteUserMessage)
                await botContext.Responder.DeleteMessageAsync(context);

            if (transition.IsFinished)
            {
                context.Session.Navigation.ActiveWizardId = null;
                await _wizardStore.DeleteAsync(context.UserId, activeWizardId, context.CancellationToken);
            }
            else
            {
                await _wizardStore.SaveAsync(context.UserId, activeWizardId, storageState, context.CancellationToken);
            }

            if (transition.EndpointResult != null)
                await transition.EndpointResult.ExecuteAsync(botContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing wizard {WizardId} for user {UserId}",
                activeWizardId, context.UserId);
            throw;
        }
    }
}