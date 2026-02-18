using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UpdateContext = TelegramBotFlow.Core.Context.UpdateContext;

namespace TelegramBotFlow.Core.Tests.ContextModel;

public sealed class UpdateContextFactsTests
{
    [Fact]
    public void MessageUpdate_ExtractsCoreFields()
    {
        var update = new Update
        {
            Message = new Message
            {
                Id = 77,
                Text = "/start referral",
                Chat = new Chat { Id = 1234, Type = ChatType.Private },
                From = new User { Id = 5678, FirstName = "Tester" },
                Date = DateTime.UtcNow
            }
        };

        IServiceProvider services = Substitute.For<IServiceProvider>();

        var context = new UpdateContext(update, services);

        Assert.Equal(UpdateType.Message, context.UpdateType);
        Assert.Equal(1234, context.ChatId);
        Assert.Equal(5678, context.UserId);
        Assert.Equal(77, context.MessageId);
        Assert.Equal("/start referral", context.MessageText);
        Assert.Equal("referral", context.CommandArgument);
        Assert.Null(context.CallbackData);
        Assert.False(context.IsAdmin);
    }

    [Fact]
    public void CallbackUpdate_ExtractsCoreFields()
    {
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "cb-1",
                Data = "nav:settings",
                From = new User { Id = 51, FirstName = "Tester" },
                Message = new Message
                {
                    Id = 909,
                    Chat = new Chat { Id = 808, Type = ChatType.Private },
                    Date = DateTime.UtcNow
                }
            }
        };

        var context = new UpdateContext(update, Substitute.For<IServiceProvider>());

        Assert.Equal(UpdateType.CallbackQuery, context.UpdateType);
        Assert.Equal(808, context.ChatId);
        Assert.Equal(51, context.UserId);
        Assert.Equal(909, context.MessageId);
        Assert.Equal("nav:settings", context.CallbackData);
        Assert.Null(context.MessageText);
        Assert.Null(context.CommandArgument);
    }
}
