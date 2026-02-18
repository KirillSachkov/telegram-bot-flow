using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ReplyMarkup = Telegram.Bot.Types.ReplyMarkups.ReplyMarkup;

namespace TelegramBotFlow.Core.Context;

public sealed class UpdateResponder : IUpdateResponder
{
    private readonly ITelegramBotClient _bot;

    public UpdateResponder(ITelegramBotClient bot)
    {
        _bot = bot;
    }

    public async Task<Message> ReplyAsync(
        UpdateContext context,
        string text,
        ReplyMarkup? replyMarkup = null,
        ParseMode parseMode = default)
    {
        return await _bot.SendMessage(
            context.ChatId,
            text,
            replyMarkup: replyMarkup,
            parseMode: parseMode,
            cancellationToken: context.CancellationToken);
    }

    public async Task EditMessageAsync(
        UpdateContext context,
        string text,
        InlineKeyboardMarkup? replyMarkup = null,
        ParseMode parseMode = default)
    {
        if (context.MessageId is null)
            return;

        await _bot.EditMessageText(
            context.ChatId,
            context.MessageId.Value,
            text,
            replyMarkup: replyMarkup,
            parseMode: parseMode,
            cancellationToken: context.CancellationToken);
    }

    public async Task EditMessageAsync(
        UpdateContext context,
        int messageId,
        string text,
        InlineKeyboardMarkup? replyMarkup = null,
        ParseMode parseMode = default)
    {
        await _bot.EditMessageText(
            context.ChatId,
            messageId,
            text,
            replyMarkup: replyMarkup,
            parseMode: parseMode,
            cancellationToken: context.CancellationToken);
    }

    public async Task DeleteMessageAsync(UpdateContext context, int messageId)
    {
        await _bot.DeleteMessage(context.ChatId, messageId, context.CancellationToken);
    }

    public async Task DeleteMessageAsync(UpdateContext context)
    {
        if (context.MessageId is null)
            return;

        await _bot.DeleteMessage(context.ChatId, context.MessageId.Value, context.CancellationToken);
    }

    public async Task AnswerCallbackAsync(UpdateContext context, string? text = null, bool showAlert = false)
    {
        if (context.Update.CallbackQuery is null)
            return;

        await _bot.AnswerCallbackQuery(
            context.Update.CallbackQuery.Id,
            text: text,
            showAlert: showAlert,
            cancellationToken: context.CancellationToken);
    }
}