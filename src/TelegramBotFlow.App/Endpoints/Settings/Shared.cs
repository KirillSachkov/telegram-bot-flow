using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.App.Endpoints.Settings;

internal static class Shared
{
    internal static ReplyKeyboardMarkup BuildSettingsKeyboard() =>
        new ReplyKeyboard()
            .Button("Язык")
            .Button("Уведомления")
            .Row()
            .Button("О боте")
            .Button("Скрыть меню")
            .Resize()
            .Build();
}
