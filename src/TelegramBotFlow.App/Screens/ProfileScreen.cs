using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Screens;

public sealed class ProfileScreen : IScreen
{
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
