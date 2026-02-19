using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;

namespace TelegramBotFlow.Core.Hosting;

/// <summary>
/// Фоновый воркер, который параллельно вычитывает обновления из канала и отправляет их в Pipeline.
/// </summary>
public sealed class UpdateProcessingWorker : BackgroundService
{
    private readonly ChannelReader<Update> _reader;
    private readonly UpdatePipeline _pipeline;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UpdateProcessingWorker> _logger;

    public UpdateProcessingWorker(
        ChannelReader<Update> reader,
        UpdatePipeline pipeline,
        IServiceScopeFactory scopeFactory,
        ILogger<UpdateProcessingWorker> logger)
    {
        _reader = reader;
        _pipeline = pipeline;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 100, // Hard limit to prevent DB pool exhaustion
            CancellationToken = stoppingToken
        };

        try
        {
            await Parallel.ForEachAsync(
                _reader.ReadAllAsync(stoppingToken),
                options,
                async (update, ct) =>
                {
                    try
                    {
                        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
                        var context = new UpdateContext(update, scope.ServiceProvider, ct);
                        await _pipeline.ProcessAsync(context);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing update {UpdateId}", update.Id);
                    }
                });
        }
        catch (OperationCanceledException)
        {
            // Штатная отмена, игнорируем
        }
    }
}
