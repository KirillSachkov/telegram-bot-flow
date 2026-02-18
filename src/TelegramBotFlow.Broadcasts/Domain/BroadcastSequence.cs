namespace TelegramBotFlow.Broadcasts.Domain;

public sealed class BroadcastSequence
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; init; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public List<BroadcastSequenceStep> Steps { get; init; } = [];
}
