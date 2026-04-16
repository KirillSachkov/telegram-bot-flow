using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Features.Profile;

/// <summary>
/// Экран профиля пользователя с базовой информацией из контекста и сессии.
/// </summary>
public sealed class ProfileScreen : IScreen
{
    /// <inheritdoc/>
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        string name = ctx.Session?.Data.GetString("name") ?? "Не указано";

        return ValueTask.FromResult(
            new ScreenView(
                    $"<b>Профиль</b>\n\n" +
                    $"ID: <code>{ctx.UserId}</code>\n" +
                    $"Имя: {name}")
                .NavigateButton<ContactScreen>("\ud83d\udcf1 Контакт")
                .Row()
                .BackButton());
    }
}