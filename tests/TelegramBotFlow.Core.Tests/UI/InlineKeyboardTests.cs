using FluentAssertions;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.Core.Tests.UI;

public sealed class InlineKeyboardTests
{
    [Fact]
    public void Build_SingleRow_CreatesCorrectMarkup()
    {
        var markup = new InlineKeyboard()
            .Button("Yes", "yes")
            .Button("No", "no")
            .Build();

        markup.InlineKeyboard.Should().HaveCount(1);
        markup.InlineKeyboard.First().Should().HaveCount(2);
    }

    [Fact]
    public void Build_MultipleRows_CreatesCorrectMarkup()
    {
        var markup = new InlineKeyboard()
            .Button("A", "a")
            .Row()
            .Button("B", "b")
            .Build();

        markup.InlineKeyboard.Should().HaveCount(2);
        markup.InlineKeyboard.First().Should().HaveCount(1);
        markup.InlineKeyboard.Last().Should().HaveCount(1);
    }

    [Fact]
    public void Build_ButtonData_IsCorrect()
    {
        var markup = new InlineKeyboard()
            .Button("Click", "data123")
            .Build();

        var button = markup.InlineKeyboard.First().First();
        button.Text.Should().Be("Click");
        button.CallbackData.Should().Be("data123");
    }

    [Fact]
    public void Build_UrlButton_HasCorrectUrl()
    {
        var markup = new InlineKeyboard()
            .Url("Visit", "https://example.com")
            .Build();

        var button = markup.InlineKeyboard.First().First();
        button.Text.Should().Be("Visit");
        button.Url.Should().Be("https://example.com");
    }

    [Fact]
    public void SingleButton_CreatesOneButtonMarkup()
    {
        var markup = InlineKeyboard.SingleButton("OK", "ok");

        markup.InlineKeyboard.Should().HaveCount(1);
        markup.InlineKeyboard.First().Should().HaveCount(1);
    }
}
