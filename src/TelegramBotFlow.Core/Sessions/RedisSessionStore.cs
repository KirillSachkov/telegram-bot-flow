using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace TelegramBotFlow.Core.Sessions;

/// <summary>
/// Хранилище сессий в Redis. Каждая сессия — отдельный Redis Hash.
/// Ключ: bot:session:{userId}
/// Системные поля: created_at, last_activity, flow_id, step_id, screen.
/// Пользовательские данные: user_data → JSON-строка.
/// </summary>
public sealed class RedisSessionStore : ISessionStore
{
    private const string KeyPrefix = "bot:session:";

    internal const string FieldCreatedAt = "created_at";
    internal const string FieldLastActivity = "last_activity";
    internal const string FieldFlowId = "current_flow_id";
    internal const string FieldStepId = "current_step_id";
    internal const string FieldScreen = "current_screen";
    internal const string FieldUserData = "user_data";

    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan? _sessionTtl;

    public RedisSessionStore(
        IConnectionMultiplexer redis,
        IOptions<RedisSessionOptions> options)
    {
        _redis = redis;
        _sessionTtl = options.Value.SessionTtl;
    }

    public async Task<UserSession> GetOrCreateAsync(long userId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = GetKey(userId);

        var entries = await db.HashGetAllAsync(key);

        if (entries.Length == 0)
        {
            var session = new UserSession(userId);
            await SaveAsync(session, cancellationToken);
            return session;
        }

        return Deserialize(userId, entries);
    }

    public async Task SaveAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = GetKey(session.UserId);

        session.LastActivity = DateTime.UtcNow;

        var entries = Serialize(session);

        var setTask = db.HashSetAsync(key, entries);
        var expireTask = _sessionTtl.HasValue
            ? db.KeyExpireAsync(key, _sessionTtl.Value)
            : Task.CompletedTask;

        await Task.WhenAll(setTask, expireTask);
    }

    internal static HashEntry[] Serialize(UserSession session)
    {
        var allData = session.GetAll();
        var userData = allData.Count > 0
            ? JsonSerializer.Serialize(allData)
            : string.Empty;

        return
        [
            new HashEntry(FieldCreatedAt, session.CreatedAt.ToString("O")),
            new HashEntry(FieldLastActivity, session.LastActivity.ToString("O")),
            new HashEntry(FieldFlowId, session.CurrentFlowId ?? string.Empty),
            new HashEntry(FieldStepId, session.CurrentStepId ?? string.Empty),
            new HashEntry(FieldScreen, session.CurrentScreen ?? string.Empty),
            new HashEntry(FieldUserData, userData),
        ];
    }

    internal static UserSession Deserialize(long userId, HashEntry[] entries)
    {
        var session = new UserSession(userId);
        var userData = RedisValue.Null;

        foreach (var entry in entries)
        {
            switch (entry.Name.ToString())
            {
                case FieldCreatedAt:
                    if (TryParseDate(entry.Value, out var ca)) session.CreatedAt = ca;
                    break;
                case FieldLastActivity:
                    if (TryParseDate(entry.Value, out var la)) session.LastActivity = la;
                    break;
                case FieldFlowId:
                    session.CurrentFlowId = NullIfEmpty(entry.Value);
                    break;
                case FieldStepId:
                    session.CurrentStepId = NullIfEmpty(entry.Value);
                    break;
                case FieldScreen:
                    session.CurrentScreen = NullIfEmpty(entry.Value);
                    break;
                case FieldUserData:
                    userData = entry.Value;
                    break;
            }
        }

        if (userData.IsNullOrEmpty) return session;

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(userData.ToString());
        if (data is null) return session;

        foreach (var (key, value) in data)
            session.Set(key, value);

        return session;
    }

    private static string GetKey(long userId) => $"{KeyPrefix}{userId}";

    private static string? NullIfEmpty(RedisValue value) =>
        value.IsNullOrEmpty ? null : value.ToString();

    private static bool TryParseDate(RedisValue value, out DateTime result) =>
        DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result);
}
