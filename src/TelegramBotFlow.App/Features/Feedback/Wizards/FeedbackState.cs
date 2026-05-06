namespace TelegramBotFlow.App.Features.Feedback.Wizards;

/// <summary>
///     State, накапливаемый шагами <see cref="FeedbackWizard"/>.
/// </summary>
public class FeedbackState
{
    public int Rating { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}
