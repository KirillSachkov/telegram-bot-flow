namespace TelegramBotFlow.Core.Data;

public sealed class BotSettings
{
    public const int SINGLETON_ID = 1;

    public int Id { get; set; } = SINGLETON_ID;
    public RoadmapMessageConfig? Roadmap { get; set; }
}

public sealed class RoadmapMessageConfig
{
    public long SourceChatId { get; set; }
    public int SourceMessageId { get; set; }
}
