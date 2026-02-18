using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Screens;

public interface IScreen
{
    Task<ScreenView> RenderAsync(UpdateContext ctx);
}
