using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Sessions.Redis;

public sealed class RedisSessionStore : ISessionStore
{
    private const string KEY_PREFIX = "bot:session:";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

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

        RedisValue value = await db.StringGetAsync(key);

        if (value.IsNullOrEmpty)
        {
            var session = new UserSession(userId);
            await SaveAsync(session, cancellationToken);
            return session;
        }

        return FromJson(userId, value!);
    }

    public async Task SaveAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        IDatabase db = _redis.GetDatabase();
        string key = GetKey(session.UserId);

        session.LastActivity = DateTime.UtcNow;

        string json = ToJson(session);

        Task setTask = db.StringSetAsync(key, json);
        Task expireTask = _sessionTtl.HasValue
            ? db.KeyExpireAsync(key, _sessionTtl.Value)
            : Task.CompletedTask;

        await Task.WhenAll(setTask, expireTask);
    }

    internal static string ToJson(UserSession session)
    {
        IReadOnlyDictionary<string, string> allData = session.GetAll();

        var payload = new SessionPayload
        {
            CreatedAt = session.CreatedAt,
            LastActivity = session.LastActivity,
            CurrentScreen = session.CurrentScreen,
            NavMessageId = session.NavMessageId,
            CurrentMediaType = session.CurrentMediaType,
            NavigationStack = session.NavigationStack.Count > 0 ? [.. session.NavigationStack] : null,
            UserData = allData.Count > 0 ? new Dictionary<string, string>(allData) : null,
            PendingInputActionId = session.PendingInputActionId,
        };

        return JsonSerializer.Serialize(payload, _jsonOptions);
    }

    internal static UserSession FromJson(long userId, string json)
    {
        SessionPayload payload = JsonSerializer.Deserialize<SessionPayload>(json, _jsonOptions)!;

        var session = new UserSession(userId)
        {
            CreatedAt = payload.CreatedAt,
            LastActivity = payload.LastActivity,
            CurrentScreen = payload.CurrentScreen,
            NavMessageId = payload.NavMessageId,
            CurrentMediaType = payload.CurrentMediaType,
            NavigationStack = payload.NavigationStack is { Count: > 0 } ? payload.NavigationStack : [],
            PendingInputActionId = payload.PendingInputActionId,
        };

        if (payload.UserData is not null)
        {
            foreach ((string key, string val) in payload.UserData)
                session.Set(key, val);
        }

        return session;
    }

    private static string GetKey(long userId) => $"{KEY_PREFIX}{userId}";

    private sealed class SessionPayload
    {
        public DateTime CreatedAt { get; init; }
        public DateTime LastActivity { get; init; }
        public string? CurrentScreen { get; init; }
        public int? NavMessageId { get; init; }
        public ScreenMediaType CurrentMediaType { get; init; }
        public List<string>? NavigationStack { get; init; }
        public Dictionary<string, string>? UserData { get; init; }
        public string? PendingInputActionId { get; init; }
    }
}
