using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Throttling;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

/// <summary>
/// Middleware для rate limiting (защита от флуда).
/// Использует System.Threading.RateLimiting для контроля количества сообщений от пользователя.
/// </summary>
public sealed class ThrottlingMiddleware : IUpdateMiddleware
{
    private readonly PartitionedRateLimiter<long> _rateLimiter;
    private readonly ThrottlingOptions _options;
    private readonly ILogger<ThrottlingMiddleware> _logger;

    public ThrottlingMiddleware(
        PartitionedRateLimiter<long> rateLimiter,
        IOptions<ThrottlingOptions> options,
        ILogger<ThrottlingMiddleware> logger)
    {
        _rateLimiter = rateLimiter;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        // Пропускаем, если нет UserId (channel posts, etc.)
        if (context.UserId == 0)
        {
            await next(context);
            return;
        }

        long userId = context.UserId;

        // Пропускаем, если пользователь в whitelist
        if (_options.WhitelistedUserIds.Contains(userId))
        {
            await next(context);
            return;
        }

        // Пытаемся получить permit для пользователя
        using RateLimitLease lease = await _rateLimiter.AcquireAsync(userId, permitCount: 1, context.CancellationToken);

        if (!lease.IsAcquired)
        {
            // Лимит превышен
            TimeSpan retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfterValue)
                ? retryAfterValue
                : TimeSpan.Zero;

            _logger.LogWarning(
                "User {UserId} exceeded rate limit. Retry after {RetryAfter}",
                userId, retryAfter);

            if (!_options.SendThrottleMessage)
                return;

            try
            {
                string message = retryAfter > TimeSpan.Zero
                    ? $"{_options.ThrottleMessage} Повторите через {retryAfter.TotalSeconds:F0} секунд."
                    : _options.ThrottleMessage;

                await context.ReplyAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send throttle message to user {UserId}", userId);
            }

            // Short-circuit: не пропускаем дальше
            return;
        }

        // Permit получен, продолжаем pipeline
        await next(context);
    }
}
