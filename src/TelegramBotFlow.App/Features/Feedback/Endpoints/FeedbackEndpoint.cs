using TelegramBotFlow.App.Features.Feedback.Wizards;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;

namespace TelegramBotFlow.App.Features.Feedback.Endpoints;

/// <summary>
///     Action-маркер для запуска <see cref="FeedbackWizard"/>.
/// </summary>
public struct StartFeedbackAction : IBotAction;

/// <summary>
///     Регистрирует кнопку-триггер запуска мастера обратной связи.
/// </summary>
public sealed class FeedbackEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app) =>
        app.MapAction<StartFeedbackAction>((UpdateContext _) => BotResults.StartWizard<FeedbackWizard>());
}
