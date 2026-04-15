using System.Text.Json;
using TelegramBotFlow.Core.Exceptions;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Sessions;

/// <summary>
/// Состояние навигации в рамках сессии пользователя.
/// Владелец: INavigationService (фреймворк). Прикладной код только читает.
/// Все сеттеры и методы мутации — internal, доступны только реализациям фреймворка
/// (TelegramBotFlow.Core, TelegramBotFlow.Core.Redis).
/// </summary>
public sealed class NavigationState
{
    /// <summary>ID текущего экрана.</summary>
    public string? CurrentScreen { get; internal set; }

    /// <summary>ID nav-сообщения Telegram (бот редактирует это сообщение при навигации).</summary>
    public int? NavMessageId { get; internal set; }

    /// <summary>Тип медиа в текущем nav-сообщении.</summary>
    public ScreenMediaType CurrentMediaType { get; internal set; } = ScreenMediaType.None;

    /// <summary>
    /// Стек истории навигации (список screen ID в порядке посещения).
    /// Используй <c>BotResults.Back()</c> вместо прямого изменения стека.
    /// </summary>
    public List<string> NavigationStack { get; internal set; } = [];

    /// <summary>
    /// ID ожидаемого input-действия. Если задан, следующее текстовое сообщение
    /// пользователя направляется в соответствующий input handler.
    /// </summary>
    public string? PendingInputActionId { get; internal set; }

    /// <summary>
    /// ID активного визарда. Если задан, все update-ы перехватываются WizardMiddleware.
    /// </summary>
    public string? ActiveWizardId { get; internal set; }

    // -- Navigation args (per-transition, cleared after screen render) --

    private readonly Dictionary<string, string> _navArgs = [];

    /// <summary>Устанавливает аргумент для передачи следующему экрану при навигации.</summary>
    public void SetNavigationArg(string key, string value) => _navArgs[key] = value;

    /// <summary>Сериализует значение в JSON и устанавливает аргумент навигации.</summary>
    public void SetNavigationArg<T>(string key, T value) =>
        _navArgs[key] = JsonSerializer.Serialize(value);

    /// <summary>Возвращает строковый аргумент навигации по ключу.</summary>
    public string? GetNavigationArg(string key) => _navArgs.GetValueOrDefault(key);

    /// <summary>Десериализует аргумент навигации в указанный тип.</summary>
    public T? GetNavigationArg<T>(string key)
    {
        if (!_navArgs.TryGetValue(key, out string? json))
            return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    internal void ClearNavigationArgs() => _navArgs.Clear();

    internal IReadOnlyDictionary<string, string> GetAllNavArgs() => _navArgs;

    internal void PopulateNavArgs(IReadOnlyDictionary<string, string> navArgs)
    {
        foreach ((string key, string value) in navArgs)
            _navArgs[key] = value;
    }

    // -- Payload store (LRU 500) --

    private const int MAX_PAYLOADS = 500;
    private readonly Dictionary<string, string> _payloads = [];
    private readonly LinkedList<string> _payloadOrder = new();

    internal void StorePayloadJson(string shortId, string json)
    {
        if (_payloads.ContainsKey(shortId))
        {
            _payloads[shortId] = json;
            return;
        }

        if (_payloads.Count >= MAX_PAYLOADS)
        {
            string oldest = _payloadOrder.First!.Value;
            _payloadOrder.RemoveFirst();
            _payloads.Remove(oldest);
        }

        _payloadOrder.AddLast(shortId);
        _payloads[shortId] = json;
    }

    internal T GetPayload<T>(string shortId)
    {
        if (_payloads.TryGetValue(shortId, out string? json))
            return JsonSerializer.Deserialize<T>(json)!;

        throw new PayloadExpiredException();
    }

    internal IReadOnlyDictionary<string, string> GetAllPayloads() => _payloads;

    internal void PopulatePayloads(IReadOnlyDictionary<string, string> payloads)
    {
        foreach ((string key, string value) in payloads)
        {
            _payloadOrder.AddLast(key);
            _payloads[key] = value;
        }
    }

    // -- Internal mutation methods (фреймворк only) --

    /// <summary>Переходит на экран, обновляя стек навигации.</summary>
    internal void PushScreen(string screenId)
    {
        PendingInputActionId = null;

        if (CurrentScreen == screenId)
            return;

        if (CurrentScreen is not null)
        {
            int existingIndex = NavigationStack.IndexOf(screenId);
            if (existingIndex >= 0)
                NavigationStack.RemoveRange(existingIndex, NavigationStack.Count - existingIndex);
            else
            {
                NavigationStack.Add(CurrentScreen);

                if (NavigationStack.Count > UserSession.MAX_NAVIGATION_DEPTH)
                    NavigationStack.RemoveRange(0, NavigationStack.Count - UserSession.MAX_NAVIGATION_DEPTH);
            }
        }

        CurrentScreen = screenId;
    }

    /// <summary>Выполняет pop из стека навигации и возвращает ID предыдущего экрана.</summary>
    internal string? PopScreen()
    {
        PendingInputActionId = null;

        if (NavigationStack.Count == 0)
            return null;

        string previous = NavigationStack[^1];
        NavigationStack.RemoveAt(NavigationStack.Count - 1);
        CurrentScreen = previous;
        return previous;
    }

    /// <summary>Устанавливает или сбрасывает ожидание текстового ввода.</summary>
    internal void SetPending(string? actionId) => PendingInputActionId = actionId;

    /// <summary>Сбрасывает текущий экран без изменения стека.</summary>
    internal void ClearCurrentScreen() => CurrentScreen = null;

    /// <summary>
    /// Сбрасывает стек навигации и текущий экран, сохраняя NavMessageId и ActiveWizardId.
    /// Используется при возврате в главное меню.
    /// </summary>
    internal void Reset()
    {
        CurrentScreen = null;
        NavigationStack.Clear();
        PendingInputActionId = null;
        ClearNavigationArgs();
        // NavMessageId и CurrentMediaType сохраняются — бот редактирует существующее сообщение
    }

    /// <summary>Полная очистка навигационного состояния.</summary>
    internal void Clear()
    {
        CurrentScreen = null;
        NavMessageId = null;
        CurrentMediaType = ScreenMediaType.None;
        NavigationStack.Clear();
        PendingInputActionId = null;
        ActiveWizardId = null;
        _navArgs.Clear();
        _payloads.Clear();
        _payloadOrder.Clear();
    }
}