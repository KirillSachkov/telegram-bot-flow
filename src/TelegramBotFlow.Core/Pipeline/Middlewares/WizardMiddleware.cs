using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TelegramBotFlow.Core.Constants;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

/// <summary>
/// Middleware перехвата апдейтов, если пользователь находится в активном визарде.
/// Должен быть зарегистрирован после SessionMiddleware, но до бизнес-логики и маршрутизаторов.
/// </summary>
public sealed class WizardMiddleware : IUpdateMiddleware
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

        string? activeWizardId = context.Session.ActiveWizardId;

        if (string.IsNullOrWhiteSpace(activeWizardId))
        {
            await next(context);
            return;
        }

        if (context.MessageText is BotCommands.CANCEL or BotCommands.START)
        {
            _logger.LogInformation("Wizard {WizardId} cancelled by user via command", activeWizardId);
            context.Session.ActiveWizardId = null;
            await _wizardStore.DeleteAsync(context.UserId, activeWizardId, context.CancellationToken);
            await next(context);
            return;
        }

        WizardStorageState? storageState =
            await _wizardStore.GetAsync(context.UserId, activeWizardId, context.CancellationToken);

        if (storageState is null)
        {
            _logger.LogWarning("Wizard {WizardId} state not found or expired", activeWizardId);
            context.Session.ActiveWizardId = null;
            await next(context);
            return;
        }

        if (!_wizardRegistry.HasWizard(activeWizardId))
        {
            _logger.LogError("Wizard implementation {WizardId} not found in registry", activeWizardId);
            context.Session.ActiveWizardId = null;
            await _wizardStore.DeleteAsync(context.UserId, activeWizardId, context.CancellationToken);
            await next(context);
            return;
        }

        IBotWizard wizardInstance = _wizardRegistry.Resolve(activeWizardId, context.RequestServices);

        try
        {
            WizardTransition transition = await wizardInstance.ProcessUpdateAsync(context, storageState);

            // Удаляем текстовое сообщение пользователя только когда шаг продвигается вперёд:
            // Stay сам удаляет своё сообщение; для callback-обновлений message — это бот-сообщение, не трогаем.
            if (context.Update.Message is not null && transition.EndpointResult is not StayResult)
            {
                IUpdateResponder responder = context.RequestServices.GetRequiredService<IUpdateResponder>();
                await responder.DeleteMessageAsync(context);
            }

            if (transition.IsFinished)
            {
                context.Session.ActiveWizardId = null;
                await _wizardStore.DeleteAsync(context.UserId, activeWizardId, context.CancellationToken);
            }
            else
            {
                await _wizardStore.SaveAsync(context.UserId, activeWizardId, storageState, context.CancellationToken);
            }

            if (transition.EndpointResult != null)
            {
                IScreenNavigator navigator = context.RequestServices.GetRequiredService<IScreenNavigator>();
                await transition.EndpointResult.ExecuteAsync(context, navigator);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing wizard {WizardId} for user {UserId}",
                activeWizardId, context.UserId);
            throw;
        }
    }
}
