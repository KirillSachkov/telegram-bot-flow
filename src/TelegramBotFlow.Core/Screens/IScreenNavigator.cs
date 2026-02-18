using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Screens;

public interface IScreenNavigator
{
    Task NavigateToAsync(UpdateContext context, string screenId);

    Task NavigateToAsync<TScreen>(UpdateContext context) where TScreen : IScreen;

    Task GoBackAsync(UpdateContext context);

    Task RefreshScreenAsync(UpdateContext context);

    Task ShowViewAsync(UpdateContext context, ScreenView view);
}