namespace TelegramBotFlow.Core.Sessions;

public sealed class RedisSessionOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Время жизни сессии в минутах. Например: 1440 = 24 часа, 10080 = 7 дней.
    /// Null или 0 — без автоистечения (сессия живёт до перезапуска Redis).
    /// </summary>
    public int? SessionTtlMinutes { get; set; }

    public TimeSpan? SessionTtl =>
        SessionTtlMinutes is > 0 ? TimeSpan.FromMinutes(SessionTtlMinutes.Value) : null;
}
