using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Wizards;

/// <summary>
/// Методы расширения для работы с визардами.
/// </summary>
public static class WizardContextExtensions
{
    /// <summary>
    /// Запускает визард заданного типа <typeparamref name="TWizard"/>.
    /// <para>
    /// Метод является тонким фасадом над <see cref="StartWizardResult"/> — вся логика
    /// инициализации живёт в <c>IEndpointResult.ExecuteAsync</c>, следуя паттерну пайплайна.
    /// В большинстве случаев предпочтительнее возвращать
    /// <c>BotResults.StartWizard&lt;TWizard&gt;()</c> из обработчика напрямую.
    /// </para>
    /// </summary>
    /// <typeparam name="TWizard">Тип визарда, наследованный от <see cref="BotWizard{TState}"/>.</typeparam>
    /// <param name="context">Текущий контекст апдейта.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public static async Task StartWizardAsync<TWizard>(
        this UpdateContext context,
        CancellationToken cancellationToken = default)
        where TWizard : class, IBotWizard
    {
        IScreenNavigator navigator = context.RequestServices.GetRequiredService<IScreenNavigator>();
        await BotResults.StartWizard<TWizard>().ExecuteAsync(context, navigator);
    }
}
