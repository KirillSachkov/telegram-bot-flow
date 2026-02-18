using FluentAssertions;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Tests.Screens;

public sealed class ScreenViewTests
{
    [Fact]
    public void Constructor_SetsText()
    {
        var view = new ScreenView("Hello");

        view.Text.Should().Be("Hello");
    }

    [Fact]
    public void NoButtons_KeyboardIsNull()
    {
        var view = new ScreenView("text");

        view.Keyboard.Should().BeNull();
    }

    [Fact]
    public void Button_AddsToKeyboard()
    {
        ScreenView view = new ScreenView("text")
            .Button("Click", "callback:1");

        view.Keyboard.Should().NotBeNull();
        view.Keyboard!.InlineKeyboard.Should().HaveCount(1);
        view.Keyboard.InlineKeyboard.First().Should().HaveCount(1);
    }

    [Fact]
    public void MultipleButtons_SameRow()
    {
        ScreenView view = new ScreenView("text")
            .Button("A", "a")
            .Button("B", "b");

        view.Keyboard.Should().NotBeNull();
        view.Keyboard!.InlineKeyboard.Should().HaveCount(1);
        view.Keyboard.InlineKeyboard.First().Should().HaveCount(2);
    }

    [Fact]
    public void Row_StartsNewRow()
    {
        ScreenView view = new ScreenView("text")
            .Button("A", "a")
            .Row()
            .Button("B", "b");

        view.Keyboard.Should().NotBeNull();
        view.Keyboard!.InlineKeyboard.Should().HaveCount(2);
    }

    [Fact]
    public void BackButton_AddsNavBackCallback()
    {
        ScreenView view = new ScreenView("text")
            .Button("Action", "do:something")
            .BackButton();

        view.Keyboard.Should().NotBeNull();
        IEnumerable<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton> lastRow =
            view.Keyboard!.InlineKeyboard.Last();
        lastRow.Should().ContainSingle();
        lastRow.First().CallbackData.Should().Be("nav:back");
        lastRow.First().Text.Should().Be("← Назад");
    }

    [Fact]
    public void BackButton_CustomText()
    {
        ScreenView view = new ScreenView("text")
            .BackButton("Go Back");

        view.Keyboard!.InlineKeyboard.Last().First().Text.Should().Be("Go Back");
    }

    [Fact]
    public void WithPhoto_SetsMediaAndType()
    {
        ScreenView view = new ScreenView("caption")
            .WithPhoto("https://example.com/photo.jpg");

        view.MediaType.Should().Be(ScreenMediaType.Photo);
        view.Media.Should().NotBeNull();
    }

    [Fact]
    public void WithVideo_SetsMediaAndType()
    {
        InputFileUrl file = InputFile.FromUri("https://example.com/video.mp4");
        ScreenView view = new ScreenView("caption")
            .WithVideo(file);

        view.MediaType.Should().Be(ScreenMediaType.Video);
        view.Media.Should().NotBeNull();
    }

    [Fact]
    public void Default_MediaTypeIsNone()
    {
        var view = new ScreenView("text");

        view.MediaType.Should().Be(ScreenMediaType.None);
        view.Media.Should().BeNull();
    }

    [Fact]
    public void UrlButton_AddsUrlButton()
    {
        ScreenView view = new ScreenView("text")
            .UrlButton("Open", "https://example.com");

        view.Keyboard.Should().NotBeNull();
        view.Keyboard!.InlineKeyboard.First().First().Url.Should().Be("https://example.com");
    }
}
