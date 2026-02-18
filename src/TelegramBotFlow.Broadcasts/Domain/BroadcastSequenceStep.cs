namespace TelegramBotFlow.Broadcasts.Domain;

public sealed class BroadcastSequenceStep
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required Guid SequenceId { get; init; }

    public required int Order { get; init; }

    public required long FromChatId { get; init; }

    public required int MessageId { get; init; }

    public required TimeSpan DelayAfterJoin { get; init; }

    public BroadcastSequence? Sequence { get; init; }
}
