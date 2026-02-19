using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Screens;

/// <summary>
/// API управления навигацией между экранами и рендером временных представлений.
/// </summary>
public interface IScreenNavigator
{
    /// <summary>
    /// Переходит к экрану по его идентификатору.
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    /// <param name="screenId">Идентификатор экрана.</param>
    Task NavigateToAsync(UpdateContext context, string screenId);

    /// <summary>
    /// Переходит к экрану по его типу.
    /// </summary>
    /// <typeparam name="TScreen">Тип целевого экрана.</typeparam>
    /// <param name="context">Контекст update-а.</param>
    Task NavigateToAsync<TScreen>(UpdateContext context) where TScreen : IScreen;

    /// <summary>
    /// Переходит к экрану по экземпляру типа во время выполнения.
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    /// <param name="screenType">Тип целевого экрана.</param>
    Task NavigateToAsync(UpdateContext context, Type screenType);

    /// <summary>
    /// Возвращается на предыдущий экран из стека навигации.
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    Task GoBackAsync(UpdateContext context);

    /// <summary>
    /// Якорный возврат назад: удаляет пользовательское сообщение и рендерит предыдущий экран.
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    /// <param name="notification">Необязательное callback-уведомление пользователю.</param>
    Task NavigateBackAsync(UpdateContext context, string? notification = null);

    /// <summary>
    /// Перерисовывает текущий экран без изменения истории навигации.
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    Task RefreshScreenAsync(UpdateContext context);

    /// <summary>
    /// Показывает произвольное представление без перехода на новый экран.
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    /// <param name="view">Представление для отображения.</param>
    Task ShowViewAsync(UpdateContext context, ScreenView view);
}