using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotFlow.Core.Flows;

public sealed class FlowStep
{
    public required string Id { get; init; }
    public required string PromptText { get; init; }
    public Func<string, string?>? Validator { get; init; }
    public ReplyMarkup? ReplyMarkup { get; init; }
}
