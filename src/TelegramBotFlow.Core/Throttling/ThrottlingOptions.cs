namespace TelegramBotFlow.Core.Throttling;

/// <summary>
/// Настройки rate limiting для защиты от флуда.
/// Использует System.Threading.RateLimiting под капотом.
/// </summary>
public sealed class ThrottlingOptions
{
    /// <summary>
    /// Максимальное количество сообщений от одного пользователя в течение временного окна.
    /// По умолчанию: 10 сообщений.
    /// </summary>
    public int PermitLimit { get; set; } = 10;

    /// <summary>
    /// Размер временного окна в секундах.
    /// По умолчанию: 60 секунд (1 минута).
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Количество сегментов в скользящем окне (для SlidingWindowRateLimiter).
    /// Большее значение = более гладкое ограничение.
    /// По умолчанию: 2.
    /// </summary>
    public int SegmentsPerWindow { get; set; } = 2;

    /// <summary>
    /// Включить ли автоматический ответ при превышении лимита.
    /// По умолчанию: true.
    /// </summary>
    public bool SendThrottleMessage { get; set; } = true;

    /// <summary>
    /// Сообщение, отправляемое пользователю при превышении лимита.
    /// </summary>
    public string ThrottleMessage { get; set; } = "⚠️ Слишком много сообщений. Подождите немного.";

    /// <summary>
    /// Список UserId, которые не подвержены throttling (например, администраторы).
    /// </summary>
    public HashSet<long> WhitelistedUserIds { get; set; } = [];
}
