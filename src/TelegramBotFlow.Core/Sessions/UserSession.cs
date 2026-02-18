using System.Text.Json;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Sessions;

public sealed class UserSession
{
    public long UserId { get; }
    public DateTime CreatedAt { get; internal set; }
    public DateTime LastActivity { get; internal set; }

    // Navigation
    public string? CurrentScreen { get; set; }
    public int? NavMessageId { get; set; }
    public ScreenMediaType CurrentMediaType { get; set; } = ScreenMediaType.None;
    public List<string> NavigationStack { get; set; } = [];

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
        _data.TryGetValue(key, out string? value) && int.TryParse(value, out int result) ? result : null;

    public long? GetLong(string key) =>
        _data.TryGetValue(key, out string? value) && long.TryParse(value, out long result) ? result : null;

    public bool GetBool(string key) =>
        _data.TryGetValue(key, out string? value) && value is "true";

    public bool Has(string key) => _data.ContainsKey(key);

    public void Remove(string key) => _data.Remove(key);

    public void Clear()
    {
        _data.Clear();
        CurrentScreen = null;
        NavMessageId = null;
        CurrentMediaType = ScreenMediaType.None;
        NavigationStack.Clear();
    }

    public T? GetState<T>() where T : class
    {
        string? json = GetString($"state:{typeof(T).Name}");
        return json is null ? null : JsonSerializer.Deserialize<T>(json);
    }

    public void SetState<T>(T state) where T : class =>
        Set($"state:{typeof(T).Name}", JsonSerializer.Serialize(state));

    public void RemoveState<T>() where T : class =>
        Remove($"state:{typeof(T).Name}");

    public const int MAX_NAVIGATION_DEPTH = 20;

    public IReadOnlyDictionary<string, string> GetAll() => _data;

    public void PushScreen(string screenId)
    {
        if (CurrentScreen == screenId)
            return;

        if (CurrentScreen is not null)
        {
            int existingIndex = NavigationStack.IndexOf(screenId);
            if (existingIndex >= 0)
            {
                NavigationStack.RemoveRange(existingIndex, NavigationStack.Count - existingIndex);
            }
            else
            {
                NavigationStack.Add(CurrentScreen);

                if (NavigationStack.Count > MAX_NAVIGATION_DEPTH)
                    NavigationStack.RemoveRange(0, NavigationStack.Count - MAX_NAVIGATION_DEPTH);
            }
        }

        CurrentScreen = screenId;
    }

    public string? PopScreen()
    {
        if (NavigationStack.Count == 0)
            return null;
        string previous = NavigationStack[^1];
        NavigationStack.RemoveAt(NavigationStack.Count - 1);
        CurrentScreen = previous;
        return previous;
    }

    public void ClearCurrentScreen() => CurrentScreen = null;
}
