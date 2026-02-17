using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ReplyMarkup = Telegram.Bot.Types.ReplyMarkups.ReplyMarkup;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Context;

public sealed class UpdateContext
{
    public Update Update { get; }
    public ITelegramBotClient Bot { get; }
    public IServiceProvider Services { get; }
    public CancellationToken CancellationToken { get; }

    public UserSession? Session { get; set; }

    public long ChatId { get; }
    public long UserId { get; }
    public int? MessageId { get; }
    public string? CallbackData { get; }
    public string? MessageText { get; }
    public UpdateType UpdateType { get; }
    public string? Screen => Session?.CurrentScreen;

    public void ClearScreen() => Session?.ClearScreen();

    private readonly Dictionary<string, object> _items = new();

    public UpdateContext(
        Update update,
        ITelegramBotClient bot,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        Update = update;
        Bot = bot;
        Services = services;
        CancellationToken = cancellationToken;

        UpdateType = update.Type;
        ChatId = ExtractChatId(update);
        UserId = ExtractUserId(update);
        MessageId = ExtractMessageId(update);
        CallbackData = update.CallbackQuery?.Data;
        MessageText = update.Message?.Text;
    }

    public T Resolve<T>() where T : notnull =>
        (T)Services.GetService(typeof(T))!;

    public void Set(string key, object value) => _items[key] = value;

    public T? Get<T>(string key) =>
        _items.TryGetValue(key, out var value) ? (T)value : default;

    public async Task ReplyAsync(
        string text,
        ReplyMarkup? replyMarkup = null,
        ParseMode parseMode = default)
    {
        await Bot.SendMessage(
            ChatId,
            text,
            replyMarkup: replyMarkup,
            parseMode: parseMode,
            cancellationToken: CancellationToken);
    }

    public async Task ReplyWithScreenAsync(
        string screen,
        string text,
        ReplyMarkup? replyMarkup = null,
        ParseMode parseMode = default)
    {
        if (Session is not null)
            Session.CurrentScreen = screen;

        await ReplyAsync(text, replyMarkup, parseMode);
    }

    public async Task EditMessageAsync(
        string text,
        InlineKeyboardMarkup? replyMarkup = null,
        ParseMode parseMode = default)
    {
        if (MessageId is null)
            return;

        await Bot.EditMessageText(
            ChatId,
            MessageId.Value,
            text,
            replyMarkup: replyMarkup,
            parseMode: parseMode,
            cancellationToken: CancellationToken);
    }

    public async Task AnswerCallbackAsync(string? text = null, bool showAlert = false)
    {
        if (Update.CallbackQuery is null)
            return;

        await Bot.AnswerCallbackQuery(
            Update.CallbackQuery.Id,
            text: text,
            showAlert: showAlert,
            cancellationToken: CancellationToken);
    }

    public async Task DeleteMessageAsync()
    {
        if (MessageId is null)
            return;

        await Bot.DeleteMessage(ChatId, MessageId.Value, CancellationToken);
    }

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
}
