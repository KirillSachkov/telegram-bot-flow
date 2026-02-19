using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Screens;

public sealed class ScreenMessageRenderer : IScreenMessageRenderer
{
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<ScreenMessageRenderer> _logger;

    public ScreenMessageRenderer(
        ITelegramBotClient bot,
        ILogger<ScreenMessageRenderer> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public async Task<Message> RenderAsync(
        UpdateContext context,
        ScreenView view,
        int? existingMessageId,
        ScreenMediaType oldMediaType,
        ScreenMediaType newMediaType)
    {
        if (existingMessageId is null)
        {
            return await SendNewAsync(context, view);
        }

        if (oldMediaType == ScreenMediaType.None && newMediaType == ScreenMediaType.None)
        {
            return await EditTextAsync(context, existingMessageId.Value, view);
        }

        if (oldMediaType == newMediaType && oldMediaType != ScreenMediaType.None)
        {
            return await EditMediaAsync(context, existingMessageId.Value, view);
        }

        await TryDeleteAsync(context, existingMessageId.Value);
        return await SendNewAsync(context, view);
    }

    private async Task<Message> SendNewAsync(UpdateContext context, ScreenView view)
    {
        if (view.MediaType == ScreenMediaType.None)
        {
            return await _bot.SendMessage(
                context.ChatId,
                view.Text,
                replyMarkup: view.Keyboard,
                parseMode: ParseMode.Html,
                cancellationToken: context.CancellationToken);
        }

        return view.MediaType switch
        {
            ScreenMediaType.Photo => await _bot.SendPhoto(
                context.ChatId,
                view.Media!,
                caption: view.Text,
                replyMarkup: view.Keyboard,
                parseMode: ParseMode.Html,
                cancellationToken: context.CancellationToken),

            ScreenMediaType.Video => await _bot.SendVideo(
                context.ChatId,
                view.Media!,
                caption: view.Text,
                replyMarkup: view.Keyboard,
                parseMode: ParseMode.Html,
                cancellationToken: context.CancellationToken),

            ScreenMediaType.Animation => await _bot.SendAnimation(
                context.ChatId,
                view.Media!,
                caption: view.Text,
                replyMarkup: view.Keyboard,
                parseMode: ParseMode.Html,
                cancellationToken: context.CancellationToken),

            ScreenMediaType.Document => await _bot.SendDocument(
                context.ChatId,
                view.Media!,
                caption: view.Text,
                replyMarkup: view.Keyboard,
                parseMode: ParseMode.Html,
                cancellationToken: context.CancellationToken),

            _ => await _bot.SendMessage(
                context.ChatId,
                view.Text,
                replyMarkup: view.Keyboard,
                parseMode: ParseMode.Html,
                cancellationToken: context.CancellationToken)
        };
    }

    private async Task<Message> EditTextAsync(UpdateContext context, int messageId, ScreenView view)
    {
        try
        {
            return await _bot.EditMessageText(
                context.ChatId,
                messageId,
                view.Text,
                replyMarkup: view.Keyboard,
                parseMode: ParseMode.Html,
                cancellationToken: context.CancellationToken);
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex) when (ex.ErrorCode == 400 && ex.Message.Contains("message is not modified"))
        {
            // Игнорируем ошибку "сообщение не изменено"
            _logger.LogDebug("Message {MessageId} is not modified, ignoring", messageId);
            return context.Update.CallbackQuery!.Message!;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to edit text message {MessageId}, sending new message", messageId);
            await TryDeleteAsync(context, messageId);
            return await SendNewAsync(context, view);
        }
    }

    private async Task<Message> EditMediaAsync(UpdateContext context, int messageId, ScreenView view)
    {
        try
        {
            InputMedia inputMedia = view.MediaType switch
            {
                ScreenMediaType.Photo => new InputMediaPhoto(view.Media!)
                {
                    Caption = view.Text,
                    ParseMode = ParseMode.Html
                },
                ScreenMediaType.Video => new InputMediaVideo(view.Media!)
                {
                    Caption = view.Text,
                    ParseMode = ParseMode.Html
                },
                ScreenMediaType.Animation => new InputMediaAnimation(view.Media!)
                {
                    Caption = view.Text,
                    ParseMode = ParseMode.Html
                },
                ScreenMediaType.Document => new InputMediaDocument(view.Media!)
                {
                    Caption = view.Text,
                    ParseMode = ParseMode.Html
                },
                _ => throw new InvalidOperationException($"Unsupported media type: {view.MediaType}")
            };

            return await _bot.EditMessageMedia(
                context.ChatId,
                messageId,
                inputMedia,
                replyMarkup: view.Keyboard,
                cancellationToken: context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to edit media message {MessageId}, sending new message", messageId);
            await TryDeleteAsync(context, messageId);
            return await SendNewAsync(context, view);
        }
    }

    private async Task TryDeleteAsync(UpdateContext context, int messageId)
    {
        try
        {
            await _bot.DeleteMessage(context.ChatId, messageId, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not delete message {MessageId} in chat {ChatId}", messageId, context.ChatId);
        }
    }
}