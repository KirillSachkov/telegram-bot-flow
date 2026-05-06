using TelegramBotFlow.App.Features.Onboarding.Wizards;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Routing;

namespace TelegramBotFlow.App.Features.Onboarding.Endpoints;

/// <summary>
///     Action-маркер для запуска визарда настройки профиля. Используется как payload
///     inline-кнопок (<see cref="ScreenView.Button{T}"/>) и роутится через
///     <see cref="ProfileSetupEndpoint"/>.
/// </summary>
public struct StartProfileSetupAction : IBotAction;

/// <summary>
///     Регистрирует кнопку-триггер запуска мастера настройки профиля
///     (<see cref="ProfileSetupWizard"/>).
/// </summary>
public sealed class ProfileSetupEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app) =>
        app.MapAction<StartProfileSetupAction>((UpdateContext _) => BotResults.StartWizard<ProfileSetupWizard>());
}
