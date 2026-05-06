using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Features.Profile.Screens;

/// <summary>
/// Demonstrates a reply keyboard with a contact request button.
/// </summary>
public sealed class ContactScreen : IScreen
{
    /// <inheritdoc/>
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx) =>
        ValueTask.FromResult(
            new ScreenView("Share your phone number:")
                .WithReplyKeyboard(kb => kb.RequestContact("📱 Share contact"))
                .BackButton());
}
