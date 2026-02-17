using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBotFlow.Core.UI;

public sealed class MenuBuilder
{
    private readonly List<BotCommand> _commands = [];

    public MenuBuilder Command(string command, string description)
    {
        _commands.Add(new BotCommand
        {
            Command = command.TrimStart('/').ToLowerInvariant(),
            Description = description
        });

        return this;
    }

    public async Task ApplyAsync(ITelegramBotClient bot, CancellationToken cancellationToken = default)
    {
        await bot.SetMyCommands(_commands, cancellationToken: cancellationToken);
    }

    public IReadOnlyList<BotCommand> Build() => _commands.AsReadOnly();
}
