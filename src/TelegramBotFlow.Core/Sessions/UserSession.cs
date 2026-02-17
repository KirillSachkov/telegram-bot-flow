namespace TelegramBotFlow.Core.Sessions;

public sealed class UserSession
{
    public long UserId { get; }
    public DateTime CreatedAt { get; internal set; }
    public DateTime LastActivity { get; internal set; }

    public string? CurrentFlowId { get; set; }
    public string? CurrentStepId { get; set; }
    public string? CurrentScreen { get; set; }

    private readonly Dictionary<string, string> _data = [];

    public UserSession(long userId)
    {
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
        LastActivity = DateTime.UtcNow;
    }

    public void Set(string key, string value) => _data[key] = value;

    public string? GetString(string key) =>
        _data.GetValueOrDefault(key);

    public int? GetInt(string key) =>
        _data.TryGetValue(key, out var value) && int.TryParse(value, out var result) ? result : null;

    public long? GetLong(string key) =>
        _data.TryGetValue(key, out var value) && long.TryParse(value, out var result) ? result : null;

    public bool GetBool(string key) =>
        _data.TryGetValue(key, out var value) && value is "true";

    public bool Has(string key) => _data.ContainsKey(key);

    public void Remove(string key) => _data.Remove(key);

    public void Clear()
    {
        _data.Clear();
        CurrentFlowId = null;
        CurrentStepId = null;
        CurrentScreen = null;
    }

    public IReadOnlyDictionary<string, string> GetAll() => _data;

    public bool IsInFlow => CurrentFlowId is not null;

    public void ClearScreen() => CurrentScreen = null;
}
