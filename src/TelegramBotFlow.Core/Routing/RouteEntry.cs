using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;

namespace TelegramBotFlow.Core.Routing;

public enum RouteType
{
    Command,
    Callback,
    Message,
    Update
}

public sealed class RouteEntry
{
    public RouteType Type { get; }
    public string? Pattern { get; }
    public Func<UpdateContext, bool>? Predicate { get; }
    public UpdateDelegate Handler { get; }

    private RouteEntry(RouteType type, UpdateDelegate handler, string? pattern, Func<UpdateContext, bool>? predicate)
    {
        Type = type;
        Handler = handler;
        Pattern = pattern;
        Predicate = predicate;
    }

    public static RouteEntry Command(string command, UpdateDelegate handler) =>
        new(RouteType.Command, handler, NormalizeCommand(command), null);

    public static RouteEntry Callback(string pattern, UpdateDelegate handler) =>
        new(RouteType.Callback, handler, pattern, null);

    public static RouteEntry Message(Func<UpdateContext, bool> predicate, UpdateDelegate handler) =>
        new(RouteType.Message, handler, null, predicate);

    public static RouteEntry Update(Func<UpdateContext, bool> predicate, UpdateDelegate handler) =>
        new(RouteType.Update, handler, null, predicate);

    public bool Matches(UpdateContext context) =>
        Type switch
        {
            RouteType.Command => MatchesCommand(context),
            RouteType.Callback => MatchesCallback(context),
            RouteType.Message or RouteType.Update => Predicate?.Invoke(context) == true,
            _ => false
        };

    private bool MatchesCommand(UpdateContext context)
    {
        if (context.MessageText is null || Pattern is null)
            return false;

        var text = context.MessageText.Trim();

        if (!text.StartsWith('/'))
            return false;

        var commandPart = text.Contains(' ')
            ? text[..text.IndexOf(' ')]
            : text;

        // Strip @botname from /command@botname
        var atIndex = commandPart.IndexOf('@');
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
            var prefix = Pattern[..^1];
            return context.CallbackData.StartsWith(prefix, StringComparison.Ordinal);
        }

        return string.Equals(context.CallbackData, Pattern, StringComparison.Ordinal);
    }

    private static string NormalizeCommand(string command) =>
        command.StartsWith('/') ? command.ToLowerInvariant() : $"/{command.ToLowerInvariant()}";
}
