using Telegram.Bot.Types.Enums;

namespace TelegramBotFlow.Core.Hosting;

public sealed class BotConfiguration
{
    public const string SECTION_NAME = "Bot";

    public required string Token { get; set; }
    public BotMode Mode { get; set; } = BotMode.POLLING;
    public string? WebhookUrl { get; set; }
    public string WebhookPath { get; set; } = "/api/bot/webhook";
    public long[] AdminUserIds { get; set; } = [];
    public long StorageChannelId { get; set; }
    public UpdateType[] AllowedUpdates { get; set; } = [UpdateType.Message, UpdateType.CallbackQuery];
    public string? WebhookSecretToken { get; set; }

    public int PayloadCacheSize { get; set; } = 500;
    public int SessionLockTimeoutSeconds { get; set; } = 10;
    public int MaxConcurrentUpdates { get; set; } = 100;
    public int MaxNavigationDepth { get; set; } = 20;
    public int UpdateChannelCapacity { get; set; } = 1000;
    public int WizardDefaultTtlMinutes { get; set; } = 60;
    public int ShutdownTimeoutSeconds { get; set; } = 30;
    public int TelegramRateLimitPerSecond { get; set; } = 25;
    public int MaxRetryOnRateLimit { get; set; } = 3;
    public string HealthCheckPath { get; set; } = "/health";
}

public enum BotMode
{
    POLLING,
    WEBHOOK
}