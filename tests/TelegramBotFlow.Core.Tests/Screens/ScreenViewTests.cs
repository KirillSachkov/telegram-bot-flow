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

        _ = view.Text.Should().Be("Hello");
    }

    [Fact]
    public void NoButtons_KeyboardIsNull()
    {
        var view = new ScreenView("text");

        _ = view.Keyboard.Should().BeNull();
    }

    [Fact]
    public void Button_AddsToKeyboard()
    {
        ScreenView view = new ScreenView("text")
            .Button("Click", "callback:1");

        _ = view.Keyboard.Should().NotBeNull();
        _ = view.Keyboard!.InlineKeyboard.Should().HaveCount(1);
        _ = view.Keyboard.InlineKeyboard.First().Should().HaveCount(1);
    }

    [Fact]
    public void MultipleButtons_SameRow()
    {
        ScreenView view = new ScreenView("text")
            .Button("A", "a")
            .Button("B", "b");

        _ = view.Keyboard.Should().NotBeNull();
        _ = view.Keyboard!.InlineKeyboard.Should().HaveCount(1);
        _ = view.Keyboard.InlineKeyboard.First().Should().HaveCount(2);
    }

    [Fact]
    public void Row_StartsNewRow()
    {
        ScreenView view = new ScreenView("text")
            .Button("A", "a")
            .Row()
            .Button("B", "b");

        _ = view.Keyboard.Should().NotBeNull();
        _ = view.Keyboard!.InlineKeyboard.Should().HaveCount(2);
    }

    [Fact]
    public void BackButton_AddsNavBackCallback()
    {
        ScreenView view = new ScreenView("text")
            .Button("Action", "do:something")
            .BackButton();

        _ = view.Keyboard.Should().NotBeNull();
        IEnumerable<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton> lastRow =
            view.Keyboard!.InlineKeyboard.Last();
        _ = lastRow.Should().ContainSingle();
        _ = lastRow.First().CallbackData.Should().Be("nav:back");
        _ = lastRow.First().Text.Should().Be("← Назад");
    }

    [Fact]
    public void BackButton_CustomText()
    {
        ScreenView view = new ScreenView("text")
            .BackButton("Go Back");

        _ = view.Keyboard!.InlineKeyboard.Last().First().Text.Should().Be("Go Back");
    }

    [Fact]
    public void WithPhoto_SetsMediaAndType()
    {
        ScreenView view = new ScreenView("caption")
            .WithPhoto("https://example.com/photo.jpg");

        _ = view.MediaType.Should().Be(ScreenMediaType.Photo);
        _ = view.Media.Should().NotBeNull();
    }

    [Fact]
    public void WithVideo_SetsMediaAndType()
    {
        InputFileUrl file = InputFile.FromUri("https://example.com/video.mp4");
        ScreenView view = new ScreenView("caption")
            .WithVideo(file);

        _ = view.MediaType.Should().Be(ScreenMediaType.Video);
        _ = view.Media.Should().NotBeNull();
    }

    [Fact]
    public void Default_MediaTypeIsNone()
    {
        var view = new ScreenView("text");

        _ = view.MediaType.Should().Be(ScreenMediaType.None);
        _ = view.Media.Should().BeNull();
    }

    [Fact]
    public void UrlButton_AddsUrlButton()
    {
        ScreenView view = new ScreenView("text")
            .UrlButton("Open", "https://example.com");

        _ = view.Keyboard.Should().NotBeNull();
        _ = view.Keyboard!.InlineKeyboard.First().First().Url.Should().Be("https://example.com");
    }
}
