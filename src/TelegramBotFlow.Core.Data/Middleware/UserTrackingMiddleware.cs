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
    private readonly BotDbContext<TUser> _db;

    public UserTrackingMiddleware(BotDbContext<TUser> db)
    {
        _db = db;
    }

    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        if (context.UserId != 0)
        {
            bool exists = await _db.Users.AnyAsync(u => u.TelegramId == context.UserId);
            if (!exists)
            {
                _db.Users.Add(new TUser { TelegramId = context.UserId });
                await _db.SaveChangesAsync();
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
