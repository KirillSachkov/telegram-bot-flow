using Microsoft.Extensions.Logging;
using Quartz;
using TelegramBotFlow.Broadcasts.Domain;
using TelegramBotFlow.Broadcasts.Infrastructure;
using TelegramBotFlow.Broadcasts.Services;

namespace TelegramBotFlow.Broadcasts.Jobs;

/// <summary>
/// Executes a single broadcast — sends the message to all non-blocked users.
/// Triggered on-demand when a broadcast is sent via API.
/// </summary>
public sealed class BroadcastExecutionJob(
    BroadcastsDbContext db,
    BroadcastSender sender,
    ILogger<BroadcastExecutionJob> logger) : IJob
{
    public const string BROADCAST_ID_KEY = "BroadcastId";

    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;

        string? broadcastIdStr = context.MergedJobDataMap.GetString(BROADCAST_ID_KEY);
        if (!Guid.TryParse(broadcastIdStr, out Guid broadcastId))
        {
            logger.LogError("BroadcastExecutionJob: invalid BroadcastId '{Id}'", broadcastIdStr);
            return;
        }

        Broadcast? broadcast = await db.Broadcasts.FindAsync([broadcastId], ct);
        if (broadcast is null)
        {
            logger.LogError("BroadcastExecutionJob: broadcast {Id} not found", broadcastId);
            return;
        }

        if (broadcast.Status != BroadcastStatus.Draft)
        {
            logger.LogWarning("BroadcastExecutionJob: broadcast {Id} has status {Status}, skipping",
                broadcastId, broadcast.Status);
            return;
        }

        broadcast.Status = BroadcastStatus.Sending;
        _ = await db.SaveChangesAsync(ct);

        logger.LogInformation("Starting broadcast {Id}", broadcastId);

        (int success, int failure) = await sender.SendToAllUsersAsync(broadcast.FromChatId, broadcast.MessageId, ct);

        broadcast.Status = BroadcastStatus.Completed;
        broadcast.SentAt = DateTime.UtcNow;
        broadcast.SuccessCount = success;
        broadcast.FailureCount = failure;
        _ = await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Broadcast {Id} completed: {Success} sent, {Failure} failed",
            broadcastId, success, failure);
    }
}
