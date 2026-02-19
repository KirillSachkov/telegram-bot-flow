using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Extensions;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.Core.Hosting;

/// <summary>
/// Центральный API конфигурации middleware, маршрутов и запуска Telegram-бота.
/// </summary>
public sealed class BotApplication
{
    private readonly WebApplication _app;
    private readonly UpdateRouter _router;
    private readonly List<Func<UpdateDelegate, UpdateDelegate>> _middlewares = [];
    private MenuBuilder? _menuBuilder;

    /// <summary>
    /// Провайдер сервисов приложения.
    /// </summary>
    public IServiceProvider Services => _app.Services;

    /// <summary>
    /// Базовое ASP.NET Core приложение для webhook- и инфраструктурных endpoint-ов.
    /// </summary>
    public WebApplication WebApp => _app;

    private BotApplication(WebApplication app, UpdateRouter router)
    {
        _app = app;
        _router = router;
    }

    /// <summary>
    /// Создаёт builder приложения бота.
    /// </summary>
    /// <param name="args">Аргументы командной строки.</param>
    /// <returns>Builder приложения бота.</returns>
    public static BotApplicationBuilder CreateBuilder(string[] args) => new(args);

    /// <summary>
    /// Собирает экземпляр приложения с регистрацией базовых сервисов фреймворка.
    /// </summary>
    /// <param name="builder">Builder приложения.</param>
    /// <returns>Собранный экземпляр приложения бота.</returns>
    public static BotApplication Build(BotApplicationBuilder builder)
    {
        builder.Services.AddTelegramBotFlow(builder.Configuration);

        WebApplication app = builder.WebAppBuilder.Build();

        UpdateRouter router = app.Services.GetRequiredService<UpdateRouter>();

        return new BotApplication(app, router);
    }

    // -- Middleware registration --

    /// <summary>
    /// Добавляет middleware-фабрику в pipeline обработки update-ов.
    /// </summary>
    /// <param name="middleware">Фабрика middleware-делегата.</param>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication Use(Func<UpdateDelegate, UpdateDelegate> middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// Добавляет middleware, резолвимый из DI-контейнера.
    /// </summary>
    /// <typeparam name="TMiddleware">Тип middleware, реализующий <see cref="IUpdateMiddleware"/>.</typeparam>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication Use<TMiddleware>() where TMiddleware : IUpdateMiddleware
    {
        _middlewares.Add(next => async context =>
        {
            TMiddleware middleware = context.RequestServices.GetRequiredService<TMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    /// <summary>
    /// Добавляет middleware глобальной обработки исключений.
    /// </summary>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication UseErrorHandling()
    {
        _middlewares.Add(next => async context =>
        {
            ErrorHandlingMiddleware middleware = context.RequestServices.GetRequiredService<ErrorHandlingMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    /// <summary>
    /// Добавляет middleware логирования обработки update-ов.
    /// </summary>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication UseLogging()
    {
        _middlewares.Add(next => async context =>
        {
            LoggingMiddleware middleware = context.RequestServices.GetRequiredService<LoggingMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    /// <summary>
    /// Добавляет middleware загрузки и сохранения пользовательской сессии.
    /// </summary>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication UseSession()
    {
        _middlewares.Add(next => async context =>
        {
            SessionMiddleware middleware = context.RequestServices.GetRequiredService<SessionMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    /// <summary>
    /// Добавляет middleware вычисления административного доступа пользователя.
    /// </summary>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication UseAccessPolicy()
    {
        _middlewares.Add(next => async context =>
        {
            AccessPolicyMiddleware middleware = context.RequestServices.GetRequiredService<AccessPolicyMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    /// <summary>
    /// Adds a middleware that intercepts incoming text messages and routes them to the
    /// registered input handler when <c>session.PendingInputActionId</c> is set.
    /// Must be added after <c>UseSession()</c>.
    /// </summary>
    public BotApplication UsePendingInput()
    {
        _middlewares.Add(next => async context =>
        {
            PendingInputMiddleware middleware = context.RequestServices.GetRequiredService<PendingInputMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    // -- Route registration (Minimal API style with DI) --

    /// <summary>
    /// Регистрирует обработчик команды.
    /// </summary>
    /// <param name="command">Текст команды с или без ведущего символа <c>/</c>.</param>
    /// <param name="handler">Делегат обработчика.</param>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication MapCommand(string command, Delegate handler)
    {
        _router.AddRoute(RouteEntry.Command(command, HandlerDelegateFactory.Create(handler)));
        return this;
    }

    /// <summary>
    /// Регистрирует обработчик callback-data по шаблону.
    /// </summary>
    /// <param name="pattern">Шаблон callback-data, включая wildcard-суффикс <c>*</c>.</param>
    /// <param name="handler">Делегат обработчика.</param>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication MapCallback(string pattern, Delegate handler)
    {
        _router.AddRoute(RouteEntry.Callback(pattern, HandlerDelegateFactory.Create(handler)));
        return this;
    }

    /// <summary>
    /// Регистрирует типизированный action-обработчик.
    /// Callback ID генерируется из имени типа <typeparamref name="TAction"/>.
    /// </summary>
    public BotApplication MapAction<TAction>(Delegate handler) where TAction : IBotAction
        => MapAction(typeof(TAction).Name, handler);

    /// <summary>
    /// Регистрирует обработчик action-кнопки.
    /// Автоматически отвечает на callback (убирает часики с кнопки),
    /// затем вызывает обработчик. Поддерживаемые типы возврата:
    /// <c>Task</c> (void) и <c>Task&lt;IEndpointResult&gt;</c>.
    /// </summary>
    public BotApplication MapAction(string callbackId, Delegate handler)
    {
        UpdateDelegate inner = HandlerDelegateFactory.Create(handler);

        _router.AddRoute(RouteEntry.Callback(callbackId, async ctx =>
        {
            IUpdateResponder responder = ctx.RequestServices.GetRequiredService<IUpdateResponder>();
            await responder.AnswerCallbackAsync(ctx);
            await inner(ctx);
        }));

        return this;
    }

    /// <summary>
    /// Регистрирует обработчик callback-группы по префиксу <c>{prefix}:*</c>.
    /// </summary>
    /// <param name="prefix">Префикс callback-data.</param>
    /// <param name="handler">Делегат обработчика группы callback.</param>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication MapCallbackGroup(string prefix, Delegate handler)
    {
        _router.AddRoute(RouteEntry.Callback($"{prefix}:*",
            HandlerDelegateFactory.CreateForCallbackGroup(handler, prefix)));
        return this;
    }

    /// <summary>
    /// Регистрирует типизированный input-обработчик.
    /// Action ID генерируется из имени типа <typeparamref name="TAction"/>.
    /// </summary>
    public BotApplication MapInput<TAction>(Delegate handler) where TAction : IBotAction
        => MapInput(typeof(TAction).Name, handler);

    /// <summary>
    /// Registers an input handler for the given <paramref name="actionId"/>.
    /// The handler is invoked when <c>session.PendingInputActionId == actionId</c> and the
    /// user sends a text message. Supported return types:
    /// <c>Task</c> (void = auto-back) and <c>Task&lt;IEndpointResult&gt;</c>.
    /// </summary>
    public BotApplication MapInput(string actionId, Delegate handler)
    {
        InputHandlerRegistry registry = Services.GetRequiredService<InputHandlerRegistry>();
        registry.Register(actionId, HandlerDelegateFactory.CreateForInput(handler, actionId));
        return this;
    }

    /// <summary>
    /// Регистрирует обработчик текстовых сообщений по предикату.
    /// </summary>
    /// <param name="predicate">Условие сопоставления update-а.</param>
    /// <param name="handler">Делегат обработчика.</param>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication MapMessage(Func<UpdateContext, bool> predicate, Delegate handler)
    {
        _router.AddRoute(RouteEntry.Message(predicate, HandlerDelegateFactory.Create(handler)));
        return this;
    }

    /// <summary>
    /// Регистрирует обработчик произвольного update-а по предикату.
    /// </summary>
    /// <param name="predicate">Условие сопоставления update-а.</param>
    /// <param name="handler">Делегат обработчика.</param>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication MapUpdate(Func<UpdateContext, bool> predicate, Delegate handler)
    {
        _router.AddRoute(RouteEntry.Update(predicate, HandlerDelegateFactory.Create(handler)));
        return this;
    }

    /// <summary>
    /// Регистрирует fallback-обработчик при отсутствии совпавшего маршрута.
    /// </summary>
    /// <param name="handler">Fallback-делегат.</param>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication MapFallback(Delegate handler)
    {
        _router.SetFallback(HandlerDelegateFactory.Create(handler));
        return this;
    }

    // -- Menu --

    /// <summary>
    /// Задаёт меню команд бота, отображаемое в Telegram.
    /// </summary>
    /// <param name="configure">Колбэк конфигурации меню.</param>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication SetMenu(Action<MenuBuilder> configure)
    {
        _menuBuilder = new MenuBuilder();
        configure(_menuBuilder);
        return this;
    }

    // -- Run --

    /// <summary>
    /// Собирает pipeline и запускает runtime бота.
    /// </summary>
    /// <returns>Задача жизненного цикла приложения бота.</returns>
    public async Task RunAsync()
    {
        var pipeline = UpdatePipeline.Build(_middlewares, _router.BuildTerminal());
        var runtime = new BotRuntime(_app);
        await runtime.RunAsync(pipeline, _menuBuilder);
    }
}

internal sealed class PipelineHolder
{
    /// <summary>
    /// Экземпляр pipeline, доступный инфраструктурным компонентам.
    /// </summary>
    public UpdatePipeline Pipeline { get; set; } = UpdatePipeline.Build([], _ => Task.CompletedTask);
}
