using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Flows;

public sealed class FlowDefinition
{
    public required string Id { get; init; }
    public required IReadOnlyList<FlowStep> Steps { get; init; }
    public required Func<UpdateContext, IReadOnlyDictionary<string, string>, Task> OnComplete { get; init; }
}
