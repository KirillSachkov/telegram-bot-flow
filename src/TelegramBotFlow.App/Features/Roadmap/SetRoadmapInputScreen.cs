using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Features.Roadmap;

/// <summary>
/// Screen that prompts the admin to send the message they want to use as the roadmap.
/// After rendering, the session awaits input routed to <see cref="SetRoadmapInput"/>.
/// </summary>
public sealed class SetRoadmapInputScreen : IScreen
{
    /// <inheritdoc/>
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
        => ValueTask.FromResult(
            new ScreenView(
                    "📥 <b>Задать Roadmap</b>\n\n" +
                    "Отправьте любое сообщение (текст, фото, видео), которое должно показываться " +
                    "пользователям по команде Roadmap.\n\n" +
                    "Бот автоматически перенесет его в безопасное хранилище, а из этого чата удалит, " +
                    "чтобы сохранить чистоту.")
                .BackButton()
                .AwaitInput<SetRoadmapInput>());
}
