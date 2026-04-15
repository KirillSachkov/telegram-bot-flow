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
    public string ErrorMessage { get; set; } = "An error occurred. Please try again later.";
    public long StorageChannelId { get; set; }
    public UpdateType[] AllowedUpdates { get; set; } = [UpdateType.Message, UpdateType.CallbackQuery];
}

public enum BotMode
{
    POLLING,
    WEBHOOK
}