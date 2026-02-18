using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Screens;

public sealed class ScreenNavigator : IScreenNavigator
{
    private readonly ScreenManager _screenManager;

    public ScreenNavigator(ScreenManager screenManager)
    {
        _screenManager = screenManager;
    }

    public async Task NavigateToAsync(UpdateContext context, string screenId)
    {
        await _screenManager.NavigateToAsync(context, screenId);
    }

    public async Task NavigateToAsync<TScreen>(UpdateContext context) where TScreen : IScreen
    {
        string screenId = ScreenRegistry.GetIdFromType(typeof(TScreen));
        await _screenManager.NavigateToAsync(context, screenId);
    }

    public async Task GoBackAsync(UpdateContext context)
    {
        string? previousScreen = context.Session?.PopScreen();
        if (previousScreen is not null)
        {
            await _screenManager.RenderScreenAsync(context, previousScreen, pushToStack: false);
        }
    }

    public async Task RefreshScreenAsync(UpdateContext context)
    {
        if (context.Session?.CurrentScreen is { } screen)
        {
            await _screenManager.RenderScreenAsync(context, screen, pushToStack: false);
        }
    }

    public async Task ShowViewAsync(UpdateContext context, ScreenView view)
    {
        await _screenManager.ShowViewAsync(context, view);
    }
}