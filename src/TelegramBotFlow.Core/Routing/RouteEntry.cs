using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;

namespace TelegramBotFlow.Core.Routing;

public enum RouteType
{
    COMMAND,
    CALLBACK,
    MESSAGE,
    UPDATE
}

public enum RoutePriority
{
    HIGH,
    NORMAL
}

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

    public static RouteEntry Command(string command, UpdateDelegate handler) =>
        new(RouteType.COMMAND, handler, NormalizeCommand(command), null);

    public static RouteEntry Callback(string pattern, UpdateDelegate handler) =>
        new(RouteType.CALLBACK, handler, pattern, null);

    public static RouteEntry Message(Func<UpdateContext, bool> predicate, UpdateDelegate handler,
        RoutePriority priority = RoutePriority.NORMAL) =>
        new(RouteType.MESSAGE, handler, null, predicate, priority);

    public static RouteEntry Update(Func<UpdateContext, bool> predicate, UpdateDelegate handler) =>
        new(RouteType.UPDATE, handler, null, predicate);

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
