using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Screens;

/// <summary>
/// Координирует рендер экранов, навигационный стек и состояние nav-сообщения.
/// </summary>
public sealed class ScreenManager
{
    private readonly ScreenRegistry _registry;
    private readonly IScreenMessageRenderer _messageRenderer;
    private readonly ILogger<ScreenManager> _logger;

    /// <summary>
    /// Создаёт менеджер экранов с реестром и рендерером сообщений.
    /// </summary>
    /// <param name="registry">Реестр экранов.</param>
    /// <param name="messageRenderer">Рендерер сообщений/медиа экрана.</param>
    /// <param name="logger">Логгер менеджера экранов.</param>
    public ScreenManager(
        ScreenRegistry registry,
        IScreenMessageRenderer messageRenderer,
        ILogger<ScreenManager> logger)
    {
        _registry = registry;
        _messageRenderer = messageRenderer;
        _logger = logger;
    }

    /// <summary>
    /// Переходит к экрану и добавляет его в навигационный стек.
    /// </summary>
    /// <param name="ctx">Контекст update-а.</param>
    /// <param name="screenId">Идентификатор целевого экрана.</param>
    public async Task NavigateToAsync(UpdateContext ctx, string screenId)
    {
        await RenderScreenAsync(ctx, screenId, pushToStack: true);
    }

    /// <summary>
    /// Показывает временное представление действия без изменения текущего экрана.
    /// Автоматически добавляет кнопку «Главное меню», если в представлении нет ни одной
    /// кнопки навигации (<see cref="ScreenView.HasNavigationButton"/> == <see langword="false"/>).
    /// </summary>
    /// <param name="ctx">Контекст update-а.</param>
    /// <param name="view">Представление для показа.</param>
    public async Task ShowViewAsync(UpdateContext ctx, ScreenView view)
    {
        if (!view.HasNavigationButton)
            view.MenuButton();

        UserSession? session = ctx.Session;
        int? existingMessageId = session?.NavMessageId;
        ScreenMediaType oldMediaType = session?.CurrentMediaType ?? ScreenMediaType.None;

        _logger.LogDebug(
            "Showing action view for user {UserId}. Old media: {OldMedia}, New media: {NewMedia}, NavMsg: {NavMsgId}",
            ctx.UserId, oldMediaType, view.MediaType, existingMessageId);

        Message sentMessage =
            await _messageRenderer.RenderAsync(ctx, view, existingMessageId, oldMediaType, view.MediaType);

        if (session is not null)
        {
            session.NavMessageId = sentMessage.Id;
            session.CurrentMediaType = view.MediaType;

            if (view.PendingInputActionId is not null)
                session.PendingInputActionId = view.PendingInputActionId;

            foreach (var kvp in view.Payloads)
                session.StorePayloadJson(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Рендерит экран по ID и синхронизирует навигационное состояние сессии.
    /// Автоматически добавляет кнопку «← Назад», если стек навигации не пуст и в
    /// представлении нет ни одной кнопки навигации (<see cref="ScreenView.HasNavigationButton"/>).
    /// </summary>
    /// <param name="ctx">Контекст update-а.</param>
    /// <param name="screenId">Идентификатор экрана.</param>
    /// <param name="pushToStack">Добавлять текущий экран в стек перед переходом.</param>
    internal async Task RenderScreenAsync(UpdateContext ctx, string screenId, bool pushToStack)
    {
        IScreen screen = _registry.Resolve(screenId, ctx.RequestServices);
        ScreenView view = await screen.RenderAsync(ctx);

        if (!view.HasNavigationButton && ctx.Session?.NavigationStack.Count > 0)
            view.BackButton();

        UserSession? session = ctx.Session;
        int? existingMessageId = session?.NavMessageId;
        ScreenMediaType oldMediaType = session?.CurrentMediaType ?? ScreenMediaType.None;
        ScreenMediaType newMediaType = view.MediaType;

        _logger.LogDebug(
            "Rendering screen '{ScreenId}' for user {UserId}. Old media: {OldMedia}, New media: {NewMedia}, NavMsg: {NavMsgId}",
            screenId, ctx.UserId, oldMediaType, newMediaType, existingMessageId);

        Message sentMessage =
            await _messageRenderer.RenderAsync(ctx, view, existingMessageId, oldMediaType, newMediaType);

        if (session is not null)
        {
            if (pushToStack)
                session.PushScreen(screenId); // PushScreen already clears PendingInputActionId
            else
                session.CurrentScreen = screenId;

            session.NavMessageId = sentMessage.Id;
            session.CurrentMediaType = newMediaType;

            // Apply pending input declared in the view (overrides PushScreen clear when intentional)
            if (view.PendingInputActionId is not null)
                session.PendingInputActionId = view.PendingInputActionId;

            foreach (var kvp in view.Payloads)
                session.StorePayloadJson(kvp.Key, kvp.Value);
        }
    }
}
