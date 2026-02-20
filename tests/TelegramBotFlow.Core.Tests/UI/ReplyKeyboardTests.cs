using FluentAssertions;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.Core.Tests.UI;

public sealed class ReplyKeyboardTests
{
    [Fact]
    public void Build_CreatesCorrectLayout()
    {
        Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup markup = new ReplyKeyboard()
            .Button("Menu")
            .Button("Help")
            .Row()
            .Button("Settings")
            .Build();

        _ = markup.Keyboard.Should().HaveCount(2);
        _ = markup.Keyboard.First().Should().HaveCount(2);
        _ = markup.Keyboard.Last().Should().HaveCount(1);
    }

    [Fact]
    public void Build_DefaultsResizeToTrue()
    {
        Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup markup = new ReplyKeyboard()
            .Button("Test")
            .Build();

        _ = markup.ResizeKeyboard.Should().BeTrue();
    }

    [Fact]
    public void OneTime_SetsFlag()
    {
        Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup markup = new ReplyKeyboard()
            .Button("Test")
            .OneTime()
            .Build();

        _ = markup.OneTimeKeyboard.Should().BeTrue();
    }
}
