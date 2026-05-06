using TelegramBotFlow.App.Features.Feedback.Wizards;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Features.Feedback.Screens;

/// <summary>
///     Экран благодарности после завершения <see cref="FeedbackWizard"/>.
/// </summary>
public sealed class FeedbackResultScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx) =>
        ValueTask.FromResult(new ScreenView(
                "🙏 <b>Спасибо за обратную связь!</b>\n\n" +
                "Ваш отзыв помогает нам становиться лучше.")
            .MenuButton());
}
