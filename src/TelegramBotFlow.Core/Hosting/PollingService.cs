using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;

namespace TelegramBotFlow.Core.Hosting;

public sealed class PollingService : BackgroundService
{
    private readonly ITelegramBotClient _bot;
    private readonly UpdatePipeline _pipeline;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PollingService> _logger;

    public PollingService(
        ITelegramBotClient bot,
        UpdatePipeline pipeline,
        IServiceScopeFactory scopeFactory,
        ILogger<PollingService> logger)
    {
        _bot = bot;
        _pipeline = pipeline;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [],
            DropPendingUpdates = true
        };

        await _bot.ReceiveAsync(
            updateHandler: (_, update, ct) => HandleUpdateAsync(update, ct),
            errorHandler: (_, exception, ct) => HandleErrorAsync(exception, ct),
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);
    }

    private async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var context = new UpdateContext(update, _bot, scope.ServiceProvider, cancellationToken);
        await _pipeline.ProcessAsync(context);
    }

    private Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Polling error");
        return Task.CompletedTask;
    }
}
