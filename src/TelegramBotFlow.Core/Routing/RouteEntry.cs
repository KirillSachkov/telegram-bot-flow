using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;

namespace TelegramBotFlow.Core.Routing;

/// <summary>
/// Тип маршрута для сопоставления update-а.
/// </summary>
public enum RouteType
{
    COMMAND,
    CALLBACK,
    MESSAGE,
    UPDATE
}

/// <summary>
/// Приоритет выполнения маршрута при сортировке.
/// </summary>
public enum RoutePriority
{
    HIGH,
    NORMAL
}

/// <summary>
/// Описание маршрута и логика его сопоставления с входящим update-ом.
/// </summary>
public sealed class RouteEntry
{
    public RouteType Type { get; }
    public string? Pattern { get; }
    public Func<UpdateContext, bool>? Predicate { get; }
    public UpdateDelegate Handler { get; }
    public RoutePriority Priority { get; }

    private RouteEntry(
        RouteType type,
        UpdateDelegate handler,
        string? pattern,
        Func<UpdateContext, bool>? predicate,
        RoutePriority priority = RoutePriority.NORMAL)
    {
        Type = type;
        Handler = handler;
        Pattern = pattern;
        Predicate = predicate;
        Priority = priority;
    }

    /// <summary>
    /// Создаёт маршрут для команды вида <c>/start</c>.
    /// </summary>
    /// <param name="command">Команда с или без префикса <c>/</c>.</param>
    /// <param name="handler">Обработчик совпавшего маршрута.</param>
    /// <returns>Маршрут командного типа.</returns>
    public static RouteEntry Command(string command, UpdateDelegate handler) =>
        new(RouteType.COMMAND, handler, NormalizeCommand(command), null);

    /// <summary>
    /// Создаёт маршрут для callback-data с поддержкой wildcard-суффикса <c>*</c>.
    /// </summary>
    /// <param name="pattern">Точное значение или префикс шаблона callback.</param>
    /// <param name="handler">Обработчик совпавшего маршрута.</param>
    /// <returns>Маршрут callback-типа.</returns>
    public static RouteEntry Callback(string pattern, UpdateDelegate handler) =>
        new(RouteType.CALLBACK, handler, pattern, null);

    /// <summary>
    /// Создаёт маршрут для текстовых сообщений на основе предиката.
    /// </summary>
    /// <param name="predicate">Условие сопоставления update-а.</param>
    /// <param name="handler">Обработчик совпавшего маршрута.</param>
    /// <param name="priority">Приоритет маршрута среди других.</param>
    /// <returns>Маршрут сообщений.</returns>
    public static RouteEntry Message(Func<UpdateContext, bool> predicate, UpdateDelegate handler,
        RoutePriority priority = RoutePriority.NORMAL) =>
        new(RouteType.MESSAGE, handler, null, predicate, priority);

    /// <summary>
    /// Создаёт универсальный маршрут по произвольному предикату update-а.
    /// </summary>
    /// <param name="predicate">Условие сопоставления update-а.</param>
    /// <param name="handler">Обработчик совпавшего маршрута.</param>
    /// <returns>Маршрут общего типа update.</returns>
    public static RouteEntry Update(Func<UpdateContext, bool> predicate, UpdateDelegate handler) =>
        new(RouteType.UPDATE, handler, null, predicate);

    /// <summary>
    /// Проверяет совпадение текущего маршрута с заданным контекстом.
    /// </summary>
    /// <param name="context">Контекст update-а.</param>
    /// <returns><see langword="true"/>, если маршрут применим.</returns>
    public bool Matches(UpdateContext context) =>
        Type switch
        {
            RouteType.COMMAND => MatchesCommand(context),
            RouteType.CALLBACK => MatchesCallback(context),
            RouteType.MESSAGE or RouteType.UPDATE => Predicate?.Invoke(context) == true,
            _ => false
        };

    private bool MatchesCommand(UpdateContext context)
    {
        if (context.MessageText is null || Pattern is null)
            return false;

        string text = context.MessageText.Trim();

        if (!text.StartsWith('/'))
            return false;

        string commandPart = text.Contains(' ')
            ? text[..text.IndexOf(' ')]
            : text;

        int atIndex = commandPart.IndexOf('@');
        if (atIndex > 0)
            commandPart = commandPart[..atIndex];

        return string.Equals(commandPart, Pattern, StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesCallback(UpdateContext context)
    {
        if (context.CallbackData is null || Pattern is null)
            return false;

        if (Pattern.EndsWith('*'))
        {
            string prefix = Pattern[..^1];
            return context.CallbackData.StartsWith(prefix, StringComparison.Ordinal);
        }

        return string.Equals(context.CallbackData, Pattern, StringComparison.Ordinal);
    }

    private static string NormalizeCommand(string command) =>
        command.StartsWith('/') ? command.ToLowerInvariant() : $"/{command.ToLowerInvariant()}";
}
