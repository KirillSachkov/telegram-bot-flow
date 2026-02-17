using TelegramBotFlow.Core.Context;
using ReplyMarkup = Telegram.Bot.Types.ReplyMarkups.ReplyMarkup;

namespace TelegramBotFlow.Core.Flows;

public sealed class FlowBuilder
{
    private readonly string _flowId;
    private readonly List<FlowStep> _steps = [];
    private Func<UpdateContext, IReadOnlyDictionary<string, string>, Task>? _onComplete;

    public FlowBuilder(string flowId)
    {
        _flowId = flowId;
    }

    public FlowBuilder Ask(
        string stepId,
        string promptText,
        Func<string, string?>? validate = null,
        ReplyMarkup? replyMarkup = null)
    {
        _steps.Add(new FlowStep
        {
            Id = stepId,
            PromptText = promptText,
            Validator = validate,
            ReplyMarkup = replyMarkup
        });

        return this;
    }

    public FlowBuilder OnComplete(Func<UpdateContext, IReadOnlyDictionary<string, string>, Task> handler)
    {
        _onComplete = handler;
        return this;
    }

    public FlowDefinition Build()
    {
        if (_steps.Count == 0)
            throw new InvalidOperationException($"Flow '{_flowId}' must have at least one step.");

        if (_onComplete is null)
            throw new InvalidOperationException($"Flow '{_flowId}' must have an OnComplete handler.");

        return new FlowDefinition
        {
            Id = _flowId,
            Steps = _steps.AsReadOnly(),
            OnComplete = _onComplete
        };
    }
}
