namespace TelegramBotFlow.Core.Context;

public interface IUserAccessPolicy
{
    bool IsAdmin(UpdateContext context);
}