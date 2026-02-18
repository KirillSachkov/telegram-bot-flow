using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ReplyMarkup = Telegram.Bot.Types.ReplyMarkups.ReplyMarkup;

namespace TelegramBotFlow.Core.Context;

public interface IUpdateResponder
{
    Task<Message> ReplyAsync(UpdateContext context, string text, ReplyMarkup? replyMarkup = null,
        ParseMode parseMode = default);

    Task EditMessageAsync(UpdateContext context, string text, InlineKeyboardMarkup? replyMarkup = null,
        ParseMode parseMode = default);

    Task EditMessageAsync(UpdateContext context, int messageId, string text, InlineKeyboardMarkup? replyMarkup = null,
        ParseMode parseMode = default);

    Task DeleteMessageAsync(UpdateContext context, int messageId);

    Task DeleteMessageAsync(UpdateContext context);

    Task AnswerCallbackAsync(UpdateContext context, string? text = null, bool showAlert = false);
}