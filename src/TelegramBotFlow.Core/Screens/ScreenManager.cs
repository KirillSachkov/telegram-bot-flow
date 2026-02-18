using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Screens;

public sealed class ScreenManager
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

    public async Task ShowViewAsync(UpdateContext ctx, ScreenView view)
    {
        UserSession? session = ctx.Session;
        int? existingMessageId = session?.NavMessageId;
        ScreenMediaType oldMediaType = session?.CurrentMediaType ?? ScreenMediaType.None;

        _logger.LogDebug(
            "Showing action view for user {UserId}. Old media: {OldMedia}, New media: {NewMedia}, NavMsg: {NavMsgId}",
            ctx.UserId, oldMediaType, view.MediaType, existingMessageId);

        Message sentMessage = await _messageRenderer.RenderAsync(ctx, view, existingMessageId, oldMediaType, view.MediaType);

        // Стек и CurrentScreen намеренно не трогаем — action view транзитивен.
        // CloseButton (nav:close) вернёт пользователя к CurrentScreen через RefreshScreenAsync.
        if (session is not null)
        {
            session.NavMessageId = sentMessage.Id;
            session.CurrentMediaType = view.MediaType;
        }
    }

    internal async Task RenderScreenAsync(UpdateContext ctx, string screenId, bool pushToStack)
    {
        IScreen screen = _registry.Resolve(screenId, ctx.RequestServices);
        ScreenView view = await screen.RenderAsync(ctx);

        UserSession? session = ctx.Session;
        int? existingMessageId = session?.NavMessageId;
        ScreenMediaType oldMediaType = session?.CurrentMediaType ?? ScreenMediaType.None;
        ScreenMediaType newMediaType = view.MediaType;

        _logger.LogDebug(
            "Rendering screen '{ScreenId}' for user {UserId}. Old media: {OldMedia}, New media: {NewMedia}, NavMsg: {NavMsgId}",
            screenId, ctx.UserId, oldMediaType, newMediaType, existingMessageId);

        Message sentMessage = await _messageRenderer.RenderAsync(ctx, view, existingMessageId, oldMediaType, newMediaType);

        if (session is not null)
        {
            if (pushToStack)
                session.PushScreen(screenId);
            else
                session.CurrentScreen = screenId;

            session.NavMessageId = sentMessage.Id;
            session.CurrentMediaType = newMediaType;
        }
    }
}
