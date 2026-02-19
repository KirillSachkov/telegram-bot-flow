using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using TelegramBotFlow.Core.Data;

namespace TelegramBotFlow.Broadcasts.Services;

public sealed class BroadcastSender(
    BotDbContext coreDb,
    ITelegramBotClient bot,
    ILogger<BroadcastSender> logger)
{
    private const int BATCH_SIZE = 25;
    private static readonly TimeSpan _delayBetweenMessages = TimeSpan.FromMilliseconds(50);

    public async Task<(int success, int failure)> SendToAllUsersAsync(
        long fromChatId,
        int messageId,
        CancellationToken ct = default)
    {
        List<long> users = await coreDb.Users
            .Where(u => !u.IsBlocked)
            .Select(u => u.TelegramId)
            .ToListAsync(ct);

        int success = 0;
        int failure = 0;

        foreach (long[] batch in users.Chunk(BATCH_SIZE))
        {
            foreach (long userId in batch)
            {
                bool sent = await TryCopyMessageAsync(userId, fromChatId, messageId, ct);
                if (sent)
                    success++;
                else
                    failure++;
            }

            await Task.Delay(_delayBetweenMessages, ct);
        }

        return (success, failure);
    }

    public async Task<bool> TryCopyMessageAsync(
        long userId,
        long fromChatId,
        int messageId,
        CancellationToken ct = default)
    {
        try
        {
            await bot.CopyMessage(userId, fromChatId, messageId, cancellationToken: ct);
            return true;
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == 403)
        {
            logger.LogWarning("User {UserId} blocked the bot, marking as blocked", userId);
            await MarkUserBlockedAsync(userId, ct);
            return false;
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == 429)
        {
            int retryAfter = ex.Parameters?.RetryAfter ?? 5;
            logger.LogWarning("Rate limited by Telegram, retrying after {Seconds}s", retryAfter);
            await Task.Delay(TimeSpan.FromSeconds(retryAfter), ct);
            return await TryCopyMessageAsync(userId, fromChatId, messageId, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to copy message to user {UserId}", userId);
            return false;
        }
    }

    private async Task MarkUserBlockedAsync(long userId, CancellationToken ct)
    {
        BotUser? user = await coreDb.Users.FindAsync([userId], ct);
        if (user is not null)
        {
            user.IsBlocked = true;
            await coreDb.SaveChangesAsync(ct);
        }
    }
}
