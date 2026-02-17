namespace TelegramBotFlow.Core.Hosting;

public sealed class BotConfiguration
{
    public const string SectionName = "Bot";

    public required string Token { get; set; }
    public BotMode Mode { get; set; } = BotMode.Polling;
    public string? WebhookUrl { get; set; }
    public string WebhookPath { get; set; } = "/api/bot/webhook";
}

public enum BotMode
{
    Polling,
    Webhook
}
