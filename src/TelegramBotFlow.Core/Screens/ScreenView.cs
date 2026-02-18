using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.Core.Screens;

public sealed class ScreenView
{
    public string Text { get; }
    public InlineKeyboardMarkup? Keyboard => _keyboard.HasButtons ? _keyboard.Build() : null;
    public InputFile? Media { get; private set; }
    public ScreenMediaType MediaType { get; private set; } = ScreenMediaType.None;

    private readonly InlineKeyboard _keyboard = new();

    public ScreenView(string text) => Text = text;

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

    // -- Media --

    public ScreenView WithPhoto(string url)
    {
        Media = InputFile.FromUri(url);
        MediaType = ScreenMediaType.Photo;
        return this;
    }

    public ScreenView WithPhoto(InputFile file)
    {
        Media = file;
        MediaType = ScreenMediaType.Photo;
        return this;
    }

    public ScreenView WithVideo(InputFile file)
    {
        Media = file;
        MediaType = ScreenMediaType.Video;
        return this;
    }

    public ScreenView WithAnimation(InputFile file)
    {
        Media = file;
        MediaType = ScreenMediaType.Animation;
        return this;
    }

    public ScreenView WithDocument(InputFile file)
    {
        Media = file;
        MediaType = ScreenMediaType.Document;
        return this;
    }

    // -- Buttons --

    public ScreenView Button(string text, string callbackData)
    {
        _keyboard.Button(text, callbackData);
        return this;
    }

    public ScreenView UrlButton(string text, string url)
    {
        _keyboard.Url(text, url);
        return this;
    }

    public ScreenView Row()
    {
        _keyboard.Row();
        return this;
    }

    public ScreenView BackButton(string text = "← Назад")
    {
        Row();
        _keyboard.Button(text, "nav:back");
        return this;
    }

    /// <summary>
    /// Кнопка для action-результатов. Перерисовывает текущий экран из стека
    /// без изменения истории навигации (в отличие от BackButton, которая делает Pop).
    /// </summary>
    public ScreenView CloseButton(string text = "← Назад")
    {
        Row();
        _keyboard.Button(text, "nav:close");
        return this;
    }

    /// <summary>
    /// Кнопка для возврата в главное меню. Очищает всю историю навигации.
    /// Используется на листовых экранах (результаты действий, информационные страницы).
    /// </summary>
    public ScreenView MenuButton(string text = "☰ Главное меню")
    {
        Row();
        _keyboard.Button(text, "nav:menu");
        return this;
    }
}
