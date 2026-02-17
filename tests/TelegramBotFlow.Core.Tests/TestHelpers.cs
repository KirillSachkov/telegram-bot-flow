using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Tests;

internal static class TestHelpers
{
    public static UpdateContext CreateMessageContext(
        string text,
        long userId = 123,
        long chatId = 456,
        IServiceProvider? services = null)
    {
        var update = new Update
        {
            Message = new Message
            {
                Text = text,
                From = new User { Id = userId, FirstName = "Test" },
                Chat = new Chat { Id = chatId, Type = ChatType.Private },
                Date = DateTime.UtcNow,
                Id = 1
            }
        };

        var bot = Substitute.For<ITelegramBotClient>();
        return new UpdateContext(update, bot, services ?? Substitute.For<IServiceProvider>());
    }

    public static UpdateContext CreateCallbackContext(
        string callbackData,
        long userId = 123,
        long chatId = 456,
        IServiceProvider? services = null)
    {
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "cb-1",
                Data = callbackData,
                From = new User { Id = userId, FirstName = "Test" },
                Message = new Message
                {
                    From = new User { Id = 999, FirstName = "Bot", IsBot = true },
                    Chat = new Chat { Id = chatId, Type = ChatType.Private },
                    Date = DateTime.UtcNow,
                    Id = 10
                }
            }
        };

        var bot = Substitute.For<ITelegramBotClient>();
        return new UpdateContext(update, bot, services ?? Substitute.For<IServiceProvider>());
    }
}
