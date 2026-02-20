using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Screens;

/// <summary>
/// Реализация навигации по экранам поверх <see cref="ScreenManager"/>.
/// </summary>
public sealed class ScreenNavigator : IScreenNavigator
{
    private readonly ScreenManager _screenManager;
    private readonly IUpdateResponder _responder;

    /// <summary>
    /// Создаёт навигатор экранов.
    /// </summary>
    /// <param name="screenManager">Менеджер экранов.</param>
    /// <param name="responder">Сервис ответов пользователю.</param>
    public ScreenNavigator(ScreenManager screenManager, IUpdateResponder responder)
    {
        _screenManager = screenManager;
        _responder = responder;
    }

    /// <inheritdoc />
    public async Task NavigateToAsync(UpdateContext context, string screenId)
    {
        await _screenManager.NavigateToAsync(context, screenId);
    }

    /// <inheritdoc />
    public async Task NavigateToAsync<TScreen>(UpdateContext context) where TScreen : IScreen
    {
        string screenId = ScreenRegistry.GetIdFromType(typeof(TScreen));
        await _screenManager.NavigateToAsync(context, screenId);
    }

    /// <inheritdoc />
    public async Task NavigateToAsync(UpdateContext context, Type screenType)
    {
        string screenId = ScreenRegistry.GetIdFromType(screenType);
        await _screenManager.NavigateToAsync(context, screenId);
    }

    /// <inheritdoc />
    public async Task GoBackAsync(UpdateContext context)
    {
        string? previousScreen = context.Session?.PopScreen();
        if (previousScreen is not null)
            await _screenManager.RenderScreenAsync(context, previousScreen, pushToStack: false);
        // Если стек пуст — молчаливый no-op: пользователь остаётся на текущем экране
    }

    /// <inheritdoc />
    public async Task NavigateBackAsync(UpdateContext context, string? notification = null)
    {
        // Используем peek (не pop) для определения экрана, затем pop —
        // чтобы при пустом стеке перерисовать CurrentScreen без его обнуления
        string? previousScreen = context.Session?.NavigationStack is { Count: > 0 }
            ? context.Session.NavigationStack[^1]
            : context.Session?.CurrentScreen;

        if (previousScreen is not null)
        {
            _ = context.Session!.PopScreen();
            await _screenManager.RenderScreenAsync(context, previousScreen, pushToStack: false);
        }

        if (notification is not null)
            await _responder.AnswerCallbackAsync(context, notification);
    }

    /// <inheritdoc />
    public async Task RefreshScreenAsync(UpdateContext context)
    {
        if (context.Session?.CurrentScreen is { } screen)
        {
            await _screenManager.RenderScreenAsync(context, screen, pushToStack: false);
        }
    }

    /// <inheritdoc />
    public async Task ShowViewAsync(UpdateContext context, ScreenView view)
    {
        await _screenManager.ShowViewAsync(context, view);
    }
}
