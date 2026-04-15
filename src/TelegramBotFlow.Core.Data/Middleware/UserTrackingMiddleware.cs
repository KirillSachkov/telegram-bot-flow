using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;

namespace TelegramBotFlow.Core.Data.Middleware;

/// <summary>
/// Middleware that tracks new users in the database.
/// Generic version — use with custom user types inheriting from BotUser.
/// </summary>
public sealed class UserTrackingMiddleware<TUser> : IUpdateMiddleware
    where TUser : BotUser, new()
{
    private static readonly ConcurrentDictionary<long, byte> _knownUsers = new();

    private readonly BotDbContext<TUser> _db;

    public UserTrackingMiddleware(BotDbContext<TUser> db)
    {
        _db = db;
    }

    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        if (context.UserId != 0)
        {
            long userId = context.UserId;
            if (!_knownUsers.ContainsKey(userId))
            {
                bool exists = await _db.Users.AnyAsync(u => u.TelegramId == userId);
                if (!exists)
                {
                    _db.Users.Add(new TUser { TelegramId = userId });
                    await _db.SaveChangesAsync();
                }

                _knownUsers.TryAdd(userId, 0);
            }
        }

        await next(context);
    }
}

/// <summary>
/// Non-generic UserTrackingMiddleware for simple bots using BotUser.
/// </summary>
public sealed class UserTrackingMiddleware : IUpdateMiddleware
{
    private readonly UserTrackingMiddleware<BotUser> _inner;

    public UserTrackingMiddleware(BotDbContext<BotUser> db)
    {
        _inner = new UserTrackingMiddleware<BotUser>(db);
    }

    public Task InvokeAsync(UpdateContext context, UpdateDelegate next)
        => _inner.InvokeAsync(context, next);
}