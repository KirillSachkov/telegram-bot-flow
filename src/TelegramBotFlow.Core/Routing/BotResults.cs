using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Routing;

/// <summary>
/// Статическая фабрика результатов обработчиков.
/// Аналог <c>Results</c> в ASP.NET Core Minimal APIs.
/// </summary>
/// <example>
/// <code>
/// app.MapAction("btn", async (BotDbContext db) => {
///     var data = await db.GetAsync();
///     return BotResults.ShowView(new ScreenView($"{data}"));
/// });
///
/// app.MapInput("id", async (UpdateContext ctx, BotDbContext db) => {
///     await db.SaveChangesAsync();
///     return BotResults.Back("✅ Сохранено");
/// });
/// </code>
/// </example>
public static class BotResults
{
    /// <summary>Показывает произвольное представление без навигации.</summary>
    public static IEndpointResult ShowView(ScreenView view) => new ShowViewResult(view);

    /// <summary>Возвращает пользователя на предыдущий экран.</summary>
    public static IEndpointResult Back(string? notification = null) => new NavigateBackResult(notification);

    /// <summary>Остаётся в текущем состоянии ввода, сохраняя ожидание.</summary>
    public static IEndpointResult Stay(string? notification = null) => new StayResult(notification);

    /// <summary>Переходит к экрану указанного типа.</summary>
    public static IEndpointResult NavigateTo<TScreen>() where TScreen : IScreen =>
        new NavigateToResult(typeof(TScreen));

    /// <summary>Перерисовывает текущий экран без изменения стека навигации.</summary>
    public static IEndpointResult Refresh(string? notification = null) => new RefreshResult(notification);
}
