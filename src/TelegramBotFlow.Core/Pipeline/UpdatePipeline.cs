using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Pipeline;

public sealed class UpdatePipeline
{
    private readonly UpdateDelegate _pipeline;

    private UpdatePipeline(UpdateDelegate pipeline)
    {
        _pipeline = pipeline;
    }

    public Task ProcessAsync(UpdateContext context) => _pipeline(context);

    public static UpdatePipeline Build(
        IReadOnlyList<Func<UpdateDelegate, UpdateDelegate>> middlewares,
        UpdateDelegate terminal)
    {
        UpdateDelegate pipeline = terminal;

        for (int i = middlewares.Count - 1; i >= 0; i--)
        {
            pipeline = middlewares[i](pipeline);
        }

        return new UpdatePipeline(pipeline);
    }
}
