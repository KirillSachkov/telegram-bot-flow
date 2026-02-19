using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Routing;

/// <summary>
/// Результат обработчика маршрута — исполняется системой, а не обработчиком.
/// Аналог <c>IActionResult</c> в ASP.NET Core.
/// </summary>
public interface IEndpointResult
{
    /// <summary>
    /// Когда <c>true</c>, <c>MapInput</c> сохраняет <c>PendingInputActionId</c> (режим ожидания).
    /// </summary>
    bool KeepPending { get; }

    Task ExecuteAsync(UpdateContext context, IScreenNavigator navigator);
}

/// <summary>
/// Показывает произвольное представление без перехода на новый экран.
/// </summary>
public sealed record ShowViewResult(ScreenView View) : IEndpointResult
{
    public bool KeepPending => false;

    public Task ExecuteAsync(UpdateContext ctx, IScreenNavigator nav) =>
        nav.ShowViewAsync(ctx, View);
}

/// <summary>
/// Возвращает пользователя на предыдущий экран (якорная навигация назад).
/// </summary>
public sealed record NavigateBackResult(string? Notification = null) : IEndpointResult
{
    /// <summary>
    /// Кэшированный экземпляр без уведомления.
    /// </summary>
    internal static readonly NavigateBackResult Default = new();
    public bool KeepPending => false;

    public Task ExecuteAsync(UpdateContext ctx, IScreenNavigator nav) =>
        nav.NavigateBackAsync(ctx, Notification);
}

/// <summary>
/// Переходит к экрану указанного типа.
/// </summary>
public sealed record NavigateToResult(Type ScreenType) : IEndpointResult
{
    public bool KeepPending => false;

    public Task ExecuteAsync(UpdateContext ctx, IScreenNavigator nav) =>
        nav.NavigateToAsync(ctx, ScreenType);
}

/// <summary>
/// Перерисовывает текущий экран без изменения стека навигации.
/// </summary>
public sealed record RefreshResult(string? Notification = null) : IEndpointResult
{
    public bool KeepPending => false;

    public async Task ExecuteAsync(UpdateContext ctx, IScreenNavigator nav)
    {
        await nav.RefreshScreenAsync(ctx);

        if (Notification is not null)
        {
            IUpdateResponder responder = ctx.RequestServices.GetRequiredService<IUpdateResponder>();
            await responder.AnswerCallbackAsync(ctx, Notification);
        }
    }
}

/// <summary>
/// Остаётся в текущем состоянии ввода — <c>PendingInputActionId</c> сохраняется.
/// Удаляет сообщение пользователя и показывает уведомление, если задано.
/// </summary>
public sealed record StayResult(string? Notification = null) : IEndpointResult
{
    public bool KeepPending => true;

    public async Task ExecuteAsync(UpdateContext ctx, IScreenNavigator nav)
    {
        IUpdateResponder responder = ctx.RequestServices.GetRequiredService<IUpdateResponder>();
        await responder.DeleteMessageAsync(ctx);

        if (Notification is not null && ctx.Update.CallbackQuery is not null)
            await responder.AnswerCallbackAsync(ctx, Notification);
    }
}

/// <summary>
/// Ничего не делает — используется для side-effect-only хэндлеров.
/// </summary>
public sealed record EmptyResult : IEndpointResult
{
    /// <summary>
    /// Кэшированный экземпляр для избежания аллокаций.
    /// </summary>
    public static readonly EmptyResult Instance = new();

    public bool KeepPending => false;

    public Task ExecuteAsync(UpdateContext ctx, IScreenNavigator nav) => Task.CompletedTask;
}

/// <summary>
/// Переходит к экрану по строковому идентификатору.
/// </summary>
public sealed record NavigateToByIdResult(string ScreenId) : IEndpointResult
{
    public bool KeepPending => false;

    public Task ExecuteAsync(UpdateContext ctx, IScreenNavigator nav) =>
        nav.NavigateToAsync(ctx, ScreenId);
}
