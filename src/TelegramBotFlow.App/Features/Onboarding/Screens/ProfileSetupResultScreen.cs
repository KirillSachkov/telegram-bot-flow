using TelegramBotFlow.App.Features.Onboarding.Wizards;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Features.Onboarding.Screens;

/// <summary>
///     Экран подтверждения после завершения <see cref="ProfileSetupWizard"/>.
///     Читает финальный <see cref="ProfileSetupState"/> из session.Data.
/// </summary>
public sealed class ProfileSetupResultScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        string name = ctx.Session?.Data.GetString("name") ?? "—";
        string age = ctx.Session?.Data.GetString("age") ?? "—";

        return ValueTask.FromResult(new ScreenView(
                $"✅ <b>Профиль сохранён!</b>\n\n" +
                $"Имя: <b>{name}</b>\n" +
                $"Возраст: <b>{age}</b>")
            .MenuButton());
    }
}
