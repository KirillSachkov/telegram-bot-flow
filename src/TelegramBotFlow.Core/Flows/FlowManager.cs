using Microsoft.Extensions.Logging;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Flows;

public sealed class FlowManager
{
    private readonly Dictionary<string, FlowDefinition> _flows = [];
    private readonly ILogger<FlowManager> _logger;

    public FlowManager(ILogger<FlowManager> logger)
    {
        _logger = logger;
    }

    public void Register(FlowDefinition flow)
    {
        _flows[flow.Id] = flow;
    }

    public async Task StartFlowAsync(UpdateContext context, string flowId)
    {
        if (!_flows.TryGetValue(flowId, out var flow))
            throw new InvalidOperationException($"Flow '{flowId}' is not registered.");

        if (context.Session is null)
            throw new InvalidOperationException("Session is required to start a flow.");

        context.Session.CurrentFlowId = flowId;
        context.Session.CurrentStepId = flow.Steps[0].Id;

        await context.ReplyAsync(flow.Steps[0].PromptText, flow.Steps[0].ReplyMarkup);
    }

    public async Task<bool> HandleInputAsync(UpdateContext context)
    {
        var session = context.Session;
        if (session?.CurrentFlowId is null || session.CurrentStepId is null)
            return false;

        if (!_flows.TryGetValue(session.CurrentFlowId, out var flow))
        {
            session.CurrentFlowId = null;
            session.CurrentStepId = null;
            return false;
        }

        var currentStep = flow.Steps.FirstOrDefault(s => s.Id == session.CurrentStepId);
        if (currentStep is null)
        {
            session.CurrentFlowId = null;
            session.CurrentStepId = null;
            return false;
        }

        var input = context.MessageText ?? string.Empty;

        if (currentStep.Validator is not null)
        {
            var error = currentStep.Validator(input);
            if (error is not null)
            {
                await context.ReplyAsync(error);
                return true;
            }
        }

        session.Set($"flow:{flow.Id}:{currentStep.Id}", input);

        var currentIndex = flow.Steps.ToList().IndexOf(currentStep);
        var nextIndex = currentIndex + 1;

        if (nextIndex < flow.Steps.Count)
        {
            var nextStep = flow.Steps[nextIndex];
            session.CurrentStepId = nextStep.Id;

            await context.ReplyAsync(nextStep.PromptText, nextStep.ReplyMarkup);
            return true;
        }

        var answers = CollectAnswers(session, flow);

        session.CurrentFlowId = null;
        session.CurrentStepId = null;

        _logger.LogInformation("Flow '{FlowId}' completed for user {UserId}", flow.Id, context.UserId);

        await flow.OnComplete(context, answers);
        return true;
    }

    public void CancelFlow(UpdateContext context)
    {
        if (context.Session is null)
            return;

        context.Session.CurrentFlowId = null;
        context.Session.CurrentStepId = null;
    }

    private static Dictionary<string, string> CollectAnswers(Sessions.UserSession session, FlowDefinition flow)
    {
        var answers = new Dictionary<string, string>();

        foreach (var step in flow.Steps)
        {
            var value = session.GetString($"flow:{flow.Id}:{step.Id}");
            if (value is not null)
                answers[step.Id] = value;
        }

        return answers;
    }
}
