using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Wizards;

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
/// Если в сессии не задан текущий экран (<c>CurrentScreen</c>), выполняется без эффекта (no-op).
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
/// Опционально удаляет сообщение пользователя (по умолчанию <c>true</c>) и показывает
/// уведомление в callback, если задано. Используйте <c>deleteMessage: false</c>, если
/// нужно только ответить на callback, не скрывая ввод пользователя.
/// </summary>
public sealed record StayResult(string? Notification = null, bool DeleteMessage = true) : IEndpointResult
{
    public bool KeepPending => true;

    public async Task ExecuteAsync(UpdateContext ctx, IScreenNavigator nav)
    {
        IUpdateResponder responder = ctx.RequestServices.GetRequiredService<IUpdateResponder>();

        // Удаляем только текстовые сообщения пользователя.
        // Для CallbackQuery message — это бот-сообщение с кнопкой, удалять нельзя.
        if (DeleteMessage && ctx.Update.Message is not null)
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

/// <summary>
/// Переходит к экрану с очисткой всей истории навигации (стек сбрасывается).
/// Целевой экран становится единственным в истории. Используется после завершения
/// визарда, после критичного действия или для «жёсткого» перехода в корень.
/// </summary>
public sealed record NavigateToRootResult(Type ScreenType) : IEndpointResult
{
    public bool KeepPending => false;

    public async Task ExecuteAsync(UpdateContext ctx, IScreenNavigator nav)
    {
        ctx.Session?.ResetNavigation();
        await nav.NavigateToAsync(ctx, ScreenType);
    }
}

/// <summary>
/// Переходит к экрану по строковому ID с очисткой всей истории навигации.
/// </summary>
public sealed record NavigateToRootByIdResult(string ScreenId) : IEndpointResult
{
    public bool KeepPending => false;

    public async Task ExecuteAsync(UpdateContext ctx, IScreenNavigator nav)
    {
        ctx.Session?.ResetNavigation();
        await nav.NavigateToAsync(ctx, ScreenId);
    }
}

/// <summary>
/// Запускает визард заданного типа. Инициализирует первый шаг и активирует визард в сессии.
/// </summary>
public sealed record StartWizardResult(Type WizardType) : IEndpointResult
{
    public bool KeepPending => false;

    public async Task ExecuteAsync(UpdateContext ctx, IScreenNavigator nav)
    {
        if (ctx.Session is null)
            throw new InvalidOperationException("Session is required to start a wizard.");

        string wizardId = WizardType.Name;
        WizardRegistry registry = ctx.RequestServices.GetRequiredService<WizardRegistry>();
        IWizardStore store = ctx.RequestServices.GetRequiredService<IWizardStore>();

        IBotWizard wizard = registry.Resolve(wizardId, ctx.RequestServices);
        WizardStorageState storageState = new();

        WizardTransition transition = await wizard.InitializeAsync(ctx, storageState);

        ctx.Session.ActiveWizardId = wizardId;
        await store.SaveAsync(ctx.UserId, wizardId, storageState, ctx.CancellationToken);

        if (transition.EndpointResult is not null)
            await transition.EndpointResult.ExecuteAsync(ctx, nav);
    }
}