using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using TelegramBotFlow.Broadcasts.Infrastructure;
using TelegramBotFlow.Broadcasts.Services;
using TelegramBotFlow.Core.Data;

namespace TelegramBotFlow.Broadcasts.Jobs;

/// <summary>
/// Processes broadcast sequences — sends messages to users based on their join time
/// and the sequence step delays.
/// Runs every minute via Quartz trigger.
/// </summary>
[DisallowConcurrentExecution]
public sealed class SequenceProcessorJob(
    BroadcastsDbContext db,
    BotDbContext coreDb,
    BroadcastSender sender,
    ILogger<SequenceProcessorJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        CancellationToken ct = context.CancellationToken;

        List<Domain.BroadcastSequence> activeSequences = await db.Sequences
            .Where(s => s.IsActive)
            .Include(s => s.Steps.OrderBy(st => st.Order))
            .ToListAsync(ct);

        if (activeSequences.Count == 0)
            return;

        DateTime now = DateTime.UtcNow;

        foreach (Domain.BroadcastSequence? sequence in activeSequences)
        {
            foreach (Domain.BroadcastSequenceStep step in sequence.Steps)
            {
                List<long> alreadySentUserIds = await db.UserSequenceProgress
                    .Where(p => p.SequenceId == sequence.Id && p.StepId == step.Id)
                    .Select(p => p.UserId)
                    .ToListAsync(ct);

                List<long> eligibleUserIds = await coreDb.Users
                    .Where(u => !u.IsBlocked)
                    .Where(u => u.JoinedAt.Add(step.DelayAfterJoin) <= now)
                    .Where(u => !alreadySentUserIds.Contains(u.TelegramId))
                    .Select(u => u.TelegramId)
                    .ToListAsync(ct);

                foreach (long userId in eligibleUserIds)
                {
                    bool sent = await sender.TryCopyMessageAsync(userId, step.FromChatId, step.MessageId, ct);

                    if (sent)
                    {
                        _ = db.UserSequenceProgress.Add(new Domain.UserSequenceProgress
                        {
                            UserId = userId,
                            SequenceId = sequence.Id,
                            StepId = step.Id
                        });

                        logger.LogInformation(
                            "Sent sequence {SequenceId} step {StepOrder} to user {UserId}",
                            sequence.Id, step.Order, userId);
                    }
                }

                _ = await db.SaveChangesAsync(ct);
            }
        }
    }
}
