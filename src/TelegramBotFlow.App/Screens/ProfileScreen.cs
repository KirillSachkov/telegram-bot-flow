using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Screens;

/// <summary>
/// Экран профиля пользователя с базовой информацией из контекста и сессии.
/// </summary>
public sealed class ProfileScreen : IScreen
{
    /// <summary>
    /// Рендерит карточку профиля и кнопку возврата.
    /// </summary>
    /// <param name="ctx">Контекст текущего update-а.</param>
    /// <returns>Экран профиля пользователя.</returns>
    public Task<ScreenView> RenderAsync(UpdateContext ctx)
    {
        string name = ctx.Session?.GetString("name") ?? "Не указано";

        ScreenView view = new ScreenView(
                $"<b>Профиль</b>\n\n" +
                $"ID: <code>{ctx.UserId}</code>\n" +
                $"Имя: {name}")
            .BackButton();

        return Task.FromResult(view);
    }
}
