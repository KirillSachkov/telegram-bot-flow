using System.Text.Json;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Sessions;

/// <summary>
/// Пользовательская сессия: key-value данные, state и состояние экранной навигации.
/// </summary>
public sealed class UserSession
{
    /// <summary>
    /// Идентификатор пользователя Telegram.
    /// </summary>
    public long UserId { get; }

    /// <summary>
    /// Время создания сессии (UTC).
    /// </summary>
    public DateTime CreatedAt { get; internal set; }

    /// <summary>
    /// Время последней активности пользователя (UTC).
    /// </summary>
    public DateTime LastActivity { get; internal set; }

    // Navigation — setters internal: изменение состояния снаружи фреймворка ломает навигацию.
    // Используй методы PushScreen/PopScreen/Clear/ResetNavigation для управления стеком.
    /// <summary>
    /// Текущий экран пользователя.
    /// </summary>
    public string? CurrentScreen { get; internal set; }

    /// <summary>
    /// Идентификатор навигационного сообщения в Telegram.
    /// </summary>
    public int? NavMessageId { get; internal set; }

    /// <summary>
    /// Тип медиа, отображённого в текущем nav-сообщении.
    /// </summary>
    public ScreenMediaType CurrentMediaType { get; internal set; } = ScreenMediaType.None;

    /// <summary>
    /// Стек истории экранов. Изменяй через <see cref="PushScreen"/> и <see cref="PopScreen"/>.
    /// </summary>
    public List<string> NavigationStack { get; internal set; } = [];

    // Pending input
    /// <summary>
    /// Идентификатор ожидаемого действия ввода пользователя.
    /// </summary>
    public string? PendingInputActionId { get; internal set; }

    private readonly Dictionary<string, string> _data = [];
    private readonly Dictionary<string, string> _navArgs = [];

    /// <summary>
    /// Создаёт новую сессию пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя Telegram.</param>
    public UserSession(long userId)
    {
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
        LastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// Сохраняет строковое значение по ключу.
    /// </summary>
    /// <param name="key">Ключ значения.</param>
    /// <param name="value">Строковое значение.</param>
    public void Set(string key, string value) => _data[key] = value;

    /// <summary>
    /// Возвращает строковое значение по ключу.
    /// </summary>
    /// <param name="key">Ключ значения.</param>
    /// <returns>Строковое значение или <see langword="null"/>.</returns>
    public string? GetString(string key) =>
        _data.GetValueOrDefault(key);

    /// <summary>
    /// Возвращает целочисленное значение по ключу.
    /// </summary>
    /// <param name="key">Ключ значения.</param>
    /// <returns>Целочисленное значение или <see langword="null"/>.</returns>
    public int? GetInt(string key) =>
        _data.TryGetValue(key, out string? value) && int.TryParse(value, out int result) ? result : null;

    /// <summary>
    /// Возвращает длинное целое значение по ключу.
    /// </summary>
    /// <param name="key">Ключ значения.</param>
    /// <returns>Значение типа <see cref="long"/> или <see langword="null"/>.</returns>
    public long? GetLong(string key) =>
        _data.TryGetValue(key, out string? value) && long.TryParse(value, out long result) ? result : null;

    /// <summary>
    /// Возвращает булево значение по ключу.
    /// </summary>
    /// <param name="key">Ключ значения.</param>
    /// <returns><see langword="true"/>, если значение равно строке <c>"true"</c>.</returns>
    public bool GetBool(string key) =>
        _data.TryGetValue(key, out string? value) && value is "true";

    /// <summary>
    /// Проверяет наличие ключа в сессии.
    /// </summary>
    /// <param name="key">Ключ значения.</param>
    /// <returns><see langword="true"/>, если ключ существует.</returns>
    public bool Has(string key) => _data.ContainsKey(key);

    /// <summary>
    /// Удаляет значение по ключу.
    /// </summary>
    /// <param name="key">Ключ значения.</param>
    public void Remove(string key) => _data.Remove(key);

    /// <summary>
    /// Полностью очищает сессионные данные и состояние навигации.
    /// Используется при команде /start для полного сброса сессии.
    /// </summary>
    public void Clear()
    {
        _data.Clear();
        _navArgs.Clear();
        CurrentScreen = null;
        NavMessageId = null;
        CurrentMediaType = ScreenMediaType.None;
        NavigationStack.Clear();
        PendingInputActionId = null;
        ActiveWizardId = null;
    }

    /// <summary>
    /// Сбрасывает состояние навигационного стека, сохраняя якорное сообщение и пользовательские данные.
    /// Используется при переходе в главное меню (nav:menu), чтобы рендер мог отредактировать
    /// существующее якорное сообщение вместо создания нового.
    /// </summary>
    public void ResetNavigation()
    {
        _navArgs.Clear();
        CurrentScreen = null;
        NavigationStack.Clear();
        PendingInputActionId = null;
    }

    /// <summary>
    /// Задаёт аргумент для следующего перехода на экран. Значение доступно в целевом экране
    /// в методе RenderAsync через <see cref="GetNavigationArg{T}"/> и автоматически
    /// очищается после отрисовки этого экрана. Используйте для передачи параметров перехода
    /// (например ID сущности для экрана просмотра).
    /// </summary>
    /// <param name="key">Ключ аргумента.</param>
    /// <param name="value">Строкое значение.</param>
    public void SetNavigationArg(string key, string value) => _navArgs[key] = value;

    /// <summary>
    /// Задаёт типизированный аргумент для следующего перехода (сериализация в JSON).
    /// </summary>
    public void SetNavigationArg<T>(string key, T value) =>
        _navArgs[key] = JsonSerializer.Serialize(value);

    /// <summary>
    /// Возвращает аргумент перехода, переданный через <see cref="SetNavigationArg"/> перед
    /// переходом на текущий экран. Аргументы очищаются после отрисовки экрана.
    /// </summary>
    /// <param name="key">Ключ аргумента.</param>
    /// <returns>Значение или <see langword="null"/> при отсутствии ключа.</returns>
    public string? GetNavigationArg(string key) => _navArgs.GetValueOrDefault(key);

    /// <summary>
    /// Возвращает типизированный аргумент перехода (десериализация из JSON).
    /// </summary>
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

    /// <summary>
    /// Очищает аргументы перехода. Вызывается фреймворком после отрисовки экрана;
    /// в коде вызывайте только при необходимости сбросить аргументы вручную.
    /// </summary>
    public void ClearNavigationArgs() => _navArgs.Clear();

    /// <summary>
    /// Получает typed-state из сессии по имени типа.
    /// </summary>
    /// <typeparam name="T">Тип состояния.</typeparam>
    /// <returns>Состояние или <see langword="null"/>.</returns>
    public T? GetState<T>() where T : class
    {
        string? json = GetString($"state:{typeof(T).Name}");
        return json is null ? null : JsonSerializer.Deserialize<T>(json);
    }

    /// <summary>
    /// Сохраняет typed-state в сессии.
    /// </summary>
    /// <typeparam name="T">Тип состояния.</typeparam>
    /// <param name="state">Экземпляр состояния.</param>
    public void SetState<T>(T state) where T : class =>
        Set($"state:{typeof(T).Name}", JsonSerializer.Serialize(state));

    /// <summary>
    /// Удаляет typed-state из сессии.
    /// </summary>
    /// <typeparam name="T">Тип состояния.</typeparam>
    public void RemoveState<T>() where T : class =>
        Remove($"state:{typeof(T).Name}");

    /// <summary>
    /// Идентификатор активного визарда. Если задан, все запросы перехватываются WizardMiddleware.
    /// </summary>
    public string? ActiveWizardId { get; internal set; }

    /// <summary>
    /// Максимальная глубина стека навигации.
    /// </summary>
    public const int MAX_NAVIGATION_DEPTH = 20;

    /// <summary>
    /// Возвращает все key-value данные сессии.
    /// </summary>
    /// <returns>Словарь сессионных данных.</returns>
    public IReadOnlyDictionary<string, string> GetAll() => _data;

    /// <summary>
    /// Переходит на экран и обновляет стек навигации.
    /// </summary>
    /// <param name="screenId">Идентификатор целевого экрана.</param>
    public void PushScreen(string screenId)
    {
        PendingInputActionId = null;

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

    /// <summary>
    /// Возвращает предыдущий экран из стека.
    /// </summary>
    /// <returns>ID предыдущего экрана или <see langword="null"/>.</returns>
    public string? PopScreen()
    {
        PendingInputActionId = null;

        if (NavigationStack.Count == 0)
            return null;
        string previous = NavigationStack[^1];
        NavigationStack.RemoveAt(NavigationStack.Count - 1);
        CurrentScreen = previous;
        return previous;
    }

    /// <summary>
    /// Сбрасывает текущий экран без изменения стека истории.
    /// </summary>
    public void ClearCurrentScreen() => CurrentScreen = null;

    public void SetPending(string? actionId) => PendingInputActionId = actionId;

    // -- Typed Payloads --

    private List<string> GetPayloadKeys()
    {
        string? keysString = GetString("_payload_keys");
        return keysString is null ? [] : [.. keysString.Split(',', StringSplitOptions.RemoveEmptyEntries)];
    }

    private void SetPayloadKeys(List<string> keys)
    {
        if (keys.Count == 0)
            Remove("_payload_keys");
        else
            Set("_payload_keys", string.Join(',', keys));
    }

    /// <summary>
    /// Сохраняет сериализованный JSON-пейлоад в сессию с поддержкой LRU-очистки (макс 500).
    /// </summary>
    public void StorePayloadJson(string shortId, string json)
    {
        string key = $"payload:{shortId}";
        List<string> keys = GetPayloadKeys();

        if (!keys.Contains(key))
        {
            keys.Add(key);
            if (keys.Count > 500)
            {
                string oldest = keys[0];
                keys.RemoveAt(0);
                Remove(oldest);
            }

            SetPayloadKeys(keys);
        }

        Set(key, json);
    }

    /// <summary>
    /// Извлекает десериализованный пейлоад из сессии.
    /// </summary>
    public T GetPayload<T>(string shortId)
    {
        string key = $"payload:{shortId}";
        if (_data.TryGetValue(key, out string? json))
        {
            return JsonSerializer.Deserialize<T>(json)!;
        }

        throw new Exceptions.PayloadExpiredException();
    }
}
