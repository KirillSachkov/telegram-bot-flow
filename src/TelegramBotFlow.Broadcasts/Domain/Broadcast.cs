namespace TelegramBotFlow.Broadcasts.Domain;

public sealed class Broadcast
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required long FromChatId { get; init; }

    public required int MessageId { get; init; }

    public BroadcastStatus Status { get; set; } = BroadcastStatus.Draft;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime? SentAt { get; set; }

    public int SuccessCount { get; set; }

    public int FailureCount { get; set; }
}

public enum BroadcastStatus
{
    Draft = 1,
    Sending = 2,
    Completed = 3
}
