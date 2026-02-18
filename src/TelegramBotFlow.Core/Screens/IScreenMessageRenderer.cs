using Telegram.Bot.Types;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Screens;

public interface IScreenMessageRenderer
{
    Task<Message> RenderAsync(
        UpdateContext context,
        ScreenView view,
        int? existingMessageId,
        ScreenMediaType oldMediaType,
        ScreenMediaType newMediaType);
}