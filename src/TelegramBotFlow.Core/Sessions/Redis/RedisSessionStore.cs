using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Sessions.Redis;

public sealed class RedisSessionStore : ISessionStore
{
    private const string KEY_PREFIX = "bot:session:";

    internal const string FieldCreatedAt = "created_at";
    internal const string FieldLastActivity = "last_activity";
    internal const string FieldScreen = "current_screen";
    internal const string FieldNavMessageId = "nav_message_id";
    internal const string FieldMediaType = "media_type";
    internal const string FieldNavigationStack = "nav_stack";
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
        IDatabase db = _redis.GetDatabase();
        string key = GetKey(userId);

        HashEntry[] entries = await db.HashGetAllAsync(key);

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
        IDatabase db = _redis.GetDatabase();
        string key = GetKey(session.UserId);

        session.LastActivity = DateTime.UtcNow;

        HashEntry[] entries = Serialize(session);

        Task setTask = db.HashSetAsync(key, entries);
        Task expireTask = _sessionTtl.HasValue
            ? db.KeyExpireAsync(key, _sessionTtl.Value)
            : Task.CompletedTask;

        await Task.WhenAll(setTask, expireTask);
    }

    internal static HashEntry[] Serialize(UserSession session)
    {
        IReadOnlyDictionary<string, string> allData = session.GetAll();
        string userData = allData.Count > 0
            ? JsonSerializer.Serialize(allData)
            : string.Empty;

        string navStack = session.NavigationStack.Count > 0
            ? JsonSerializer.Serialize(session.NavigationStack)
            : string.Empty;

        return
        [
            new HashEntry(FieldCreatedAt, session.CreatedAt.ToString("O")),
            new HashEntry(FieldLastActivity, session.LastActivity.ToString("O")),
            new HashEntry(FieldScreen, session.CurrentScreen ?? string.Empty),
            new HashEntry(FieldNavMessageId, session.NavMessageId?.ToString() ?? string.Empty),
            new HashEntry(FieldMediaType, session.CurrentMediaType.ToString()),
            new HashEntry(FieldNavigationStack, navStack),
            new HashEntry(FieldUserData, userData),
        ];
    }

    internal static UserSession Deserialize(long userId, HashEntry[] entries)
    {
        var session = new UserSession(userId);
        RedisValue userData = RedisValue.Null;

        foreach (HashEntry entry in entries)
        {
            switch (entry.Name.ToString())
            {
                case FieldCreatedAt:
                    if (TryParseDate(entry.Value, out DateTime ca))
                        session.CreatedAt = ca;
                    break;
                case FieldLastActivity:
                    if (TryParseDate(entry.Value, out DateTime la))
                        session.LastActivity = la;
                    break;
                case FieldScreen:
                    session.CurrentScreen = NullIfEmpty(entry.Value);
                    break;
                case FieldNavMessageId:
                    string? navMsgStr = NullIfEmpty(entry.Value);
                    if (navMsgStr is not null && int.TryParse(navMsgStr, out int navMsgId))
                        session.NavMessageId = navMsgId;
                    break;
                case FieldMediaType:
                    string? mediaStr = NullIfEmpty(entry.Value);
                    if (mediaStr is not null && Enum.TryParse<ScreenMediaType>(mediaStr, out ScreenMediaType mediaType))
                        session.CurrentMediaType = mediaType;
                    break;
                case FieldNavigationStack:
                    string? stackStr = NullIfEmpty(entry.Value);
                    if (stackStr is not null)
                    {
                        List<string>? stack = JsonSerializer.Deserialize<List<string>>(stackStr);
                        if (stack is not null)
                            session.NavigationStack = stack;
                    }
                    break;
                case FieldUserData:
                    userData = entry.Value;
                    break;
            }
        }

        if (userData.IsNullOrEmpty)
            return session;

        Dictionary<string, string>? data = JsonSerializer.Deserialize<Dictionary<string, string>>(userData.ToString());
        if (data is null)
            return session;

        foreach ((string key, string value) in data)
            session.Set(key, value);

        return session;
    }

    private static string GetKey(long userId) => $"{KEY_PREFIX}{userId}";

    private static string? NullIfEmpty(RedisValue value) =>
        value.IsNullOrEmpty ? null : value.ToString();

    private static bool TryParseDate(RedisValue value, out DateTime result) =>
        DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out result);
}
