using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Screens;

/// <summary>
/// Координирует рендер экранов, навигационный стек и состояние nav-сообщения.
/// Вызывается только из <see cref="NavigationService"/> — не является публичным API.
/// </summary>
internal sealed class ScreenManager
{
    private readonly ScreenRegistry _registry;
    private readonly IScreenMessageRenderer _messageRenderer;
    private readonly ILogger<ScreenManager> _logger;

    public ScreenManager(
        ScreenRegistry registry,
        IScreenMessageRenderer messageRenderer,
        ILogger<ScreenManager> logger)
    {
        _registry = registry;
        _messageRenderer = messageRenderer;
        _logger = logger;
    }

    public async Task NavigateToAsync(UpdateContext ctx, string screenId)
    {
        await RenderScreenAsync(ctx, screenId, pushToStack: true);
    }

    /// <summary>
    /// Показывает временное представление без изменения текущего экрана.
    /// Автоматически добавляет кнопку «Главное меню», если в представлении нет
    /// ни одной кнопки навигации.
    /// </summary>
    public async Task ShowViewAsync(UpdateContext ctx, ScreenView view)
    {
        if (!view.HasNavigationButton)
            view.MenuButton();

        UserSession? session = ctx.Session;
        int? existingMessageId = session?.Navigation.NavMessageId;
        ScreenMediaType oldMediaType = session?.Navigation.CurrentMediaType ?? ScreenMediaType.None;

        _logger.LogDebug(
            "Showing action view for user {UserId}. Old media: {OldMedia}, New media: {NewMedia}, NavMsg: {NavMsgId}",
            ctx.UserId, oldMediaType, view.MediaType, existingMessageId);

        var (keyboard, payloads) = view.BuildKeyboardWithPayloads();

        Message sentMessage =
            await _messageRenderer.RenderAsync(ctx, view, keyboard, existingMessageId, oldMediaType, view.MediaType);

        if (session is not null)
        {
            session.Navigation.NavMessageId = sentMessage.Id;
            session.Navigation.CurrentMediaType = view.MediaType;

            if (view.PendingInputActionId is not null)
                session.Navigation.PendingInputActionId = view.PendingInputActionId;

            foreach (KeyValuePair<string, string> kvp in payloads)
                session.Navigation.StorePayloadJson(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Рендерит экран по ID и синхронизирует навигационное состояние сессии.
    /// Автоматически добавляет кнопку «← Назад», если стек навигации не пуст и в
    /// представлении нет ни одной кнопки навигации.
    /// </summary>
    internal async Task RenderScreenAsync(UpdateContext ctx, string screenId, bool pushToStack)
    {
        IScreen screen = _registry.Resolve(screenId, ctx.RequestServices);
        ScreenView view = await screen.RenderAsync(ctx);

        if (!view.HasNavigationButton && ctx.Session?.Navigation.NavigationStack.Count > 0)
            view.BackButton();

        UserSession? session = ctx.Session;
        int? existingMessageId = session?.Navigation.NavMessageId;
        ScreenMediaType oldMediaType = session?.Navigation.CurrentMediaType ?? ScreenMediaType.None;
        ScreenMediaType newMediaType = view.MediaType;

        _logger.LogDebug(
            "Rendering screen '{ScreenId}' for user {UserId}. Old media: {OldMedia}, New media: {NewMedia}, NavMsg: {NavMsgId}",
            screenId, ctx.UserId, oldMediaType, newMediaType, existingMessageId);

        var (keyboard, payloads) = view.BuildKeyboardWithPayloads();

        Message sentMessage =
            await _messageRenderer.RenderAsync(ctx, view, keyboard, existingMessageId, oldMediaType, newMediaType);

        if (session is not null)
        {
            if (pushToStack)
                session.Navigation.PushScreen(screenId); // PushScreen clears PendingInputActionId
            else
                session.Navigation.CurrentScreen = screenId;

            session.Navigation.NavMessageId = sentMessage.Id;
            session.Navigation.CurrentMediaType = newMediaType;

            // View может явно задать pending input (перекрывает сброс из PushScreen)
            if (view.PendingInputActionId is not null)
                session.Navigation.PendingInputActionId = view.PendingInputActionId;

            foreach (KeyValuePair<string, string> kvp in payloads)
                session.Navigation.StorePayloadJson(kvp.Key, kvp.Value);

            session.Navigation.ClearNavigationArgs();
        }
    }
}