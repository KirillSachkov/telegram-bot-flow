using System.Threading.RateLimiting;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Throttling;

namespace TelegramBotFlow.Core.Tests.Throttling;

public sealed class ThrottlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_PassesThroughWhenNoUserId()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter(permitLimit: 10);
        var middleware = CreateMiddleware(rateLimiter);
        var bot = Substitute.For<ITelegramBotClient>();
        // Update без From (channel post, etc.) → UserId = 0
        var update = new Update
        {
            ChannelPost = new Message { Chat = new Chat { Id = 123, Type = Telegram.Bot.Types.Enums.ChatType.Channel } }
        };
        var context = new UpdateContext(update, bot, Substitute.For<IServiceProvider>());
        var nextCalled = false;

        // Act
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_PassesThroughWhitelistedUser()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter(permitLimit: 10);
        var options = new ThrottlingOptions { WhitelistedUserIds = { 123 } };
        var middleware = CreateMiddleware(rateLimiter, options);
        var bot = Substitute.For<ITelegramBotClient>();
        var update = new Update
        {
            Message = new Message
            {
                From = new User { Id = 123 },
                Chat = new Chat { Id = 123 }
            }
        };
        var context = new UpdateContext(update, bot, Substitute.For<IServiceProvider>());
        var nextCalled = false;

        // Act
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_PassesThroughWhenUnderLimit()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter(permitLimit: 5);
        var middleware = CreateMiddleware(rateLimiter);
        var bot = Substitute.For<ITelegramBotClient>();
        var update = new Update
        {
            Message = new Message
            {
                From = new User { Id = 123 },
                Chat = new Chat { Id = 123 }
            }
        };
        var context = new UpdateContext(update, bot, Substitute.For<IServiceProvider>());
        var nextCalled = false;

        // Act
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_BlocksWhenLimitExceeded()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter(permitLimit: 2);
        var middleware = CreateMiddleware(rateLimiter, sendMessage: false);
        var bot = Substitute.For<ITelegramBotClient>();
        var update = new Update
        {
            Message = new Message
            {
                From = new User { Id = 123 },
                Chat = new Chat { Id = 123 }
            }
        };
        var context = new UpdateContext(update, bot, Substitute.For<IServiceProvider>());

        // Act — первые 2 сообщения проходят, третье блокируется
        var call1 = await CallMiddleware(middleware, context);
        var call2 = await CallMiddleware(middleware, context);
        var call3 = await CallMiddleware(middleware, context);

        // Assert
        call1.Should().BeTrue();
        call2.Should().BeTrue();
        call3.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_IsolatesUserLimits()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter(permitLimit: 2);
        var middleware = CreateMiddleware(rateLimiter, sendMessage: false);
        var bot = Substitute.For<ITelegramBotClient>();

        var user1Update = new Update
        {
            Message = new Message { From = new User { Id = 123 }, Chat = new Chat { Id = 123 } }
        };
        var user2Update = new Update
        {
            Message = new Message { From = new User { Id = 456 }, Chat = new Chat { Id = 456 } }
        };

        var context1 = new UpdateContext(user1Update, bot, Substitute.For<IServiceProvider>());
        var context2 = new UpdateContext(user2Update, bot, Substitute.For<IServiceProvider>());

        // Act — каждый пользователь имеет свой лимит
        var user1Call1 = await CallMiddleware(middleware, context1);
        var user1Call2 = await CallMiddleware(middleware, context1);
        var user1Call3 = await CallMiddleware(middleware, context1); // Блокируется
        var user2Call1 = await CallMiddleware(middleware, context2); // Проходит (свой лимит)

        // Assert
        user1Call1.Should().BeTrue();
        user1Call2.Should().BeTrue();
        user1Call3.Should().BeFalse();
        user2Call1.Should().BeTrue();
    }

    // Helper methods

    private static PartitionedRateLimiter<long> CreateRateLimiter(int permitLimit)
    {
        return PartitionedRateLimiter.Create<long, long>(userId =>
        {
            return RateLimitPartition.GetSlidingWindowLimiter(userId, _ =>
                new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(60),
                    SegmentsPerWindow = 2,
                    QueueLimit = 0
                });
        });
    }

    private static ThrottlingMiddleware CreateMiddleware(
        PartitionedRateLimiter<long> rateLimiter,
        ThrottlingOptions? options = null,
        bool sendMessage = false)
    {
        options ??= new ThrottlingOptions { SendThrottleMessage = sendMessage };
        return new ThrottlingMiddleware(
            rateLimiter,
            Options.Create(options),
            NullLogger<ThrottlingMiddleware>.Instance);
    }

    private static async Task<bool> CallMiddleware(ThrottlingMiddleware middleware, UpdateContext context)
    {
        var nextCalled = false;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        return nextCalled;
    }
}
