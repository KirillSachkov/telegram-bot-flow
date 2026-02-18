namespace TelegramBotFlow.Broadcasts.Domain;

public sealed class UserSequenceProgress
{
    public required long UserId { get; init; }

    public required Guid SequenceId { get; init; }

    public required Guid StepId { get; init; }

    public DateTime SentAt { get; init; } = DateTime.UtcNow;
}
