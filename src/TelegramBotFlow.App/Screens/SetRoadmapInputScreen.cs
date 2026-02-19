using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Screens;

/// <summary>Маркер действия ввода Roadmap-сообщения.</summary>
public struct SetRoadmapInput : IBotAction;

/// <summary>
/// Screen that prompts the admin to send the message they want to use as the roadmap.
/// After rendering, the session awaits input routed to <see cref="SetRoadmapInput"/>.
/// </summary>
public sealed class SetRoadmapInputScreen : IScreen
{
    /// <summary>
    /// Рендерит экран с запросом сообщения и включает ожидание пользовательского ввода.
    /// </summary>
    /// <param name="ctx">Контекст текущего update-а.</param>
    /// <returns>Экран запроса roadmap-сообщения.</returns>
    public Task<ScreenView> RenderAsync(UpdateContext ctx)
    {
        ScreenView view = new ScreenView(
                "📥 <b>Задать Roadmap</b>\n\n" +
                "Перешлите или отправьте любое сообщение, которое должно показываться " +
                "пользователям по команде Roadmap.\n\n" +
                "Это может быть текст, фото, видео или любой другой тип сообщения.")
            .BackButton()
            .AwaitInput<SetRoadmapInput>();

        return Task.FromResult(view);
    }
}
