using System.Text.Json;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.Core.Screens;

/// <summary>
/// Builder представления экрана: текст, медиа и inline-клавиатура.
/// </summary>
public sealed class ScreenView
{
    /// <summary>
    /// Текст контента экрана.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Сформированная inline-клавиатура или <see langword="null"/>, если кнопок нет.
    /// </summary>
    public InlineKeyboardMarkup? Keyboard => _keyboard.HasButtons ? _keyboard.Build() : null;

    /// <summary>
    /// Медиа-файл экрана, если задан.
    /// </summary>
    public InputFile? Media { get; private set; }

    /// <summary>
    /// Тип прикреплённого медиа.
    /// </summary>
    public ScreenMediaType MediaType { get; private set; } = ScreenMediaType.None;

    /// <summary>
    /// When set, the bot will store this action ID in the session as PendingInputActionId
    /// after rendering this screen, enabling the next text message to be routed to the
    /// corresponding input handler.
    /// </summary>
    public string? PendingInputActionId { get; private set; }

    /// <summary>
    /// <see langword="true"/> если в представлении уже есть кнопка навигации
    /// (<see cref="BackButton"/>, <see cref="CloseButton"/> или <see cref="MenuButton"/>).
    /// Используется <c>ScreenManager</c> для автоматического добавления кнопки,
    /// когда разработчик не добавил её явно.
    /// </summary>
    public bool HasNavigationButton { get; private set; }

    private readonly InlineKeyboard _keyboard = new();
    private readonly Dictionary<string, string> _payloads = [];

    /// <summary>
    /// Сохранённые payload для кнопок текущего экрана (ShortId -> Json).
    /// </summary>
    public IReadOnlyDictionary<string, string> Payloads => _payloads;

    /// <summary>
    /// Создаёт представление экрана с текстом.
    /// </summary>
    /// <param name="text">Текст экрана.</param>
    public ScreenView(string text) => Text = text;

    /// <summary>
    /// Marks this view as awaiting text input. After render, the session's
    /// <c>PendingInputActionId</c> is set to <paramref name="actionId"/>, and the next
    /// non-command message from the user will be routed to the registered input handler.
    /// </summary>
    public ScreenView AwaitInput(string actionId)
    {
        PendingInputActionId = actionId;
        return this;
    }

    /// <summary>
    /// Типизированная версия <see cref="AwaitInput(string)"/>.
    /// Action ID генерируется из имени типа <typeparamref name="TAction"/>.
    /// </summary>
    public ScreenView AwaitInput<TAction>() where TAction : IBotAction
    {
        PendingInputActionId = typeof(TAction).Name;
        return this;
    }

    // -- Navigation buttons --

    /// <summary>
    /// Adds an inline button that navigates to the specified screen.
    /// Generates callback data: <c>nav:{screenId}</c>.
    /// </summary>
    public ScreenView NavigateButton<TScreen>(string text) where TScreen : IScreen
    {
        string screenId = ScreenRegistry.GetIdFromType(typeof(TScreen));
        _keyboard.Button(text, $"nav:{screenId}");
        return this;
    }

    // -- Action buttons --

    /// <summary>
    /// Добавляет типизированную кнопку действия.
    /// Callback ID генерируется из имени типа <typeparamref name="TAction"/>.
    /// </summary>
    public ScreenView Button<TAction>(string text) where TAction : IBotAction
    {
        _keyboard.Button(text, typeof(TAction).Name);
        return this;
    }

    /// <summary>
    /// Добавляет типизированную кнопку действия с передачей объекта (Payload).
    /// Генерирует ShortId, сохраняет payload (до 500 последних) и создаёт кнопку 
    /// с callback_data вида <c>TAction:s:ShortId</c>. Если payload короткий,
    /// он встраивается прямо в кнопку <c>TAction:j:{json}</c>.
    /// </summary>
    public ScreenView Button<TAction, TPayload>(string text, TPayload payload) where TAction : IBotAction
    {
        string json = JsonSerializer.Serialize(payload);
        string prefix = typeof(TAction).Name;

        int byteCount = System.Text.Encoding.UTF8.GetByteCount($"{prefix}:j:{json}");
        if (byteCount <= 64)
        {
            _keyboard.Button(text, $"{prefix}:j:{json}");
        }
        else
        {
            string shortId = Guid.NewGuid().ToString("N")[..8];
            _payloads[shortId] = json;
            _keyboard.Button(text, $"{prefix}:s:{shortId}");
        }

        return this;
    }

    // -- Media --

    /// <summary>
    /// Добавляет фото по URL.
    /// </summary>
    /// <param name="url">Ссылка на изображение.</param>
    /// <returns>Текущее представление для fluent-конфигурации.</returns>
    public ScreenView WithPhoto(string url)
    {
        Media = InputFile.FromUri(url);
        MediaType = ScreenMediaType.Photo;
        return this;
    }

    /// <summary>
    /// Добавляет фото из Telegram InputFile.
    /// </summary>
    /// <param name="file">Файл изображения.</param>
    /// <returns>Текущее представление для fluent-конфигурации.</returns>
    public ScreenView WithPhoto(InputFile file)
    {
        Media = file;
        MediaType = ScreenMediaType.Photo;
        return this;
    }

    /// <summary>
    /// Добавляет видео.
    /// </summary>
    /// <param name="file">Видео-файл.</param>
    /// <returns>Текущее представление для fluent-конфигурации.</returns>
    public ScreenView WithVideo(InputFile file)
    {
        Media = file;
        MediaType = ScreenMediaType.Video;
        return this;
    }

    /// <summary>
    /// Добавляет анимацию.
    /// </summary>
    /// <param name="file">Файл анимации.</param>
    /// <returns>Текущее представление для fluent-конфигурации.</returns>
    public ScreenView WithAnimation(InputFile file)
    {
        Media = file;
        MediaType = ScreenMediaType.Animation;
        return this;
    }

    /// <summary>
    /// Добавляет документ.
    /// </summary>
    /// <param name="file">Файл документа.</param>
    /// <returns>Текущее представление для fluent-конфигурации.</returns>
    public ScreenView WithDocument(InputFile file)
    {
        Media = file;
        MediaType = ScreenMediaType.Document;
        return this;
    }

    // -- Buttons --

    /// <summary>
    /// Добавляет callback-кнопку.
    /// </summary>
    /// <param name="text">Текст кнопки.</param>
    /// <param name="callbackData">Данные callback.</param>
    /// <returns>Текущее представление для fluent-конфигурации.</returns>
    public ScreenView Button(string text, string callbackData)
    {
        _keyboard.Button(text, callbackData);
        return this;
    }

    /// <summary>
    /// Добавляет URL-кнопку.
    /// </summary>
    /// <param name="text">Текст кнопки.</param>
    /// <param name="url">URL для перехода.</param>
    /// <returns>Текущее представление для fluent-конфигурации.</returns>
    public ScreenView UrlButton(string text, string url)
    {
        _keyboard.Url(text, url);
        return this;
    }

    /// <summary>
    /// Начинает новую строку в клавиатуре.
    /// </summary>
    /// <returns>Текущее представление для fluent-конфигурации.</returns>
    public ScreenView Row()
    {
        _keyboard.Row();
        return this;
    }

    /// <summary>
    /// Добавляет кнопку возврата на предыдущий экран из стека.
    /// </summary>
    /// <param name="text">Текст кнопки.</param>
    /// <returns>Текущее представление для fluent-конфигурации.</returns>
    public ScreenView BackButton(string text = "← Назад")
    {
        HasNavigationButton = true;
        Row();
        _keyboard.Button(text, NavCallbacks.BACK);
        return this;
    }

    /// <summary>
    /// Кнопка для action-результатов. Перерисовывает текущий экран из стека
    /// без изменения истории навигации (в отличие от BackButton, которая делает Pop).
    /// </summary>
    public ScreenView CloseButton(string text = "← Назад")
    {
        HasNavigationButton = true;
        Row();
        _keyboard.Button(text, NavCallbacks.CLOSE);
        return this;
    }

    /// <summary>
    /// Кнопка для возврата в главное меню. Очищает всю историю навигации.
    /// Используется на листовых экранах (результаты действий, информационные страницы).
    /// </summary>
    public ScreenView MenuButton(string text = "☰ Главное меню")
    {
        HasNavigationButton = true;
        Row();
        _keyboard.Button(text, NavCallbacks.MENU);
        return this;
    }
}

/// <summary>
/// Строковые идентификаторы системных навигационных callback-ов (<c>nav:*</c>).
/// Используй вместо magic strings везде, где нужно сослаться на навигационный callback вручную.
/// </summary>
public static class NavCallbacks
{
    /// <summary>Возврат на предыдущий экран (pop стека навигации).</summary>
    public const string BACK = "nav:back";

    /// <summary>Закрытие action-view без изменения стека (refresh текущего экрана).</summary>
    public const string CLOSE = "nav:close";

    /// <summary>Переход в главное меню с очисткой всей истории навигации.</summary>
    public const string MENU = "nav:menu";
}
