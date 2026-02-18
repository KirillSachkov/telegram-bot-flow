using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Context;

public sealed class UpdateContext
{
    public Update Update { get; }
    internal IServiceProvider RequestServices { get; }
    public CancellationToken CancellationToken { get; }

    public UserSession? Session { get; set; }
    public bool IsAdmin { get; set; }

    public long ChatId { get; }
    public long UserId { get; }
    public int? MessageId { get; }
    public string? CallbackData { get; }
    public string? MessageText { get; }
    public string? CommandArgument { get; }
    public UpdateType UpdateType { get; }
    public string? Screen => Session?.CurrentScreen;

    public UpdateContext(
        Update update,
        IServiceProvider requestServices,
        CancellationToken cancellationToken = default)
    {
        Update = update;
        RequestServices = requestServices;
        CancellationToken = cancellationToken;

        UpdateType = update.Type;
        ChatId = ExtractChatId(update);
        UserId = ExtractUserId(update);
        MessageId = ExtractMessageId(update);
        CallbackData = update.CallbackQuery?.Data;
        MessageText = update.Message?.Text;
        CommandArgument = ExtractCommandArgument(MessageText);
    }

    // -- Extractors --

    private static long ExtractChatId(Update update) =>
        update.Type switch
        {
            UpdateType.Message => update.Message!.Chat.Id,
            UpdateType.CallbackQuery => update.CallbackQuery!.Message!.Chat.Id,
            UpdateType.EditedMessage => update.EditedMessage!.Chat.Id,
            UpdateType.ChannelPost => update.ChannelPost!.Chat.Id,
            _ => 0
        };

    private static long ExtractUserId(Update update) =>
        update.Type switch
        {
            UpdateType.Message => update.Message!.From!.Id,
            UpdateType.CallbackQuery => update.CallbackQuery!.From.Id,
            UpdateType.EditedMessage => update.EditedMessage!.From!.Id,
            _ => 0
        };

    private static int? ExtractMessageId(Update update) =>
        update.Type switch
        {
            UpdateType.Message => update.Message!.Id,
            UpdateType.CallbackQuery => update.CallbackQuery!.Message?.Id,
            UpdateType.EditedMessage => update.EditedMessage!.Id,
            _ => null
        };

    private static string? ExtractCommandArgument(string? messageText)
    {
        if (messageText is null || !messageText.StartsWith('/'))
            return null;

        int spaceIndex = messageText.IndexOf(' ');
        if (spaceIndex < 0)
            return null;

        string argument = messageText[(spaceIndex + 1)..].Trim();
        return argument.Length > 0 ? argument : null;
    }
}
