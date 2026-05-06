namespace TelegramBotFlow.App.Features.Onboarding.Wizards;

/// <summary>
///     State, накапливаемый шагами <see cref="ProfileSetupWizard"/>.
///     Сериализуется в session и переживает между шагами.
/// </summary>
public class ProfileSetupState
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}
