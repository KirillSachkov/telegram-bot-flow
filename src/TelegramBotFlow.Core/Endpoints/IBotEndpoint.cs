using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.Core.Endpoints;

/// <summary>
/// Контракт endpoint-класса с декларативной регистрацией маршрутов бота.
/// </summary>
public interface IBotEndpoint
{
    /// <summary>
    /// Регистрирует маршруты endpoint-а в приложении бота.
    /// </summary>
    /// <param name="app">Экземпляр приложения бота.</param>
    void MapEndpoint(BotApplication app);
}
