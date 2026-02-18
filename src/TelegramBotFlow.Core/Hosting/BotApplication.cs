using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Extensions;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.Core.Hosting;

public sealed class BotApplication
{
    private readonly WebApplication _app;
    private readonly UpdateRouter _router;
    private readonly List<Func<UpdateDelegate, UpdateDelegate>> _middlewares = [];
    private MenuBuilder? _menuBuilder;

    public IServiceProvider Services => _app.Services;

    public WebApplication WebApp => _app;

    private BotApplication(WebApplication app, UpdateRouter router)
    {
        _app = app;
        _router = router;
    }

    public static BotApplicationBuilder CreateBuilder(string[] args) => new(args);

    public static BotApplication Build(BotApplicationBuilder builder)
    {
        builder.Services.AddTelegramBotFlow(builder.Configuration);

        WebApplication app = builder.WebAppBuilder.Build();

        UpdateRouter router = app.Services.GetRequiredService<UpdateRouter>();

        return new BotApplication(app, router);
    }

    // -- Middleware registration --

    public BotApplication Use(Func<UpdateDelegate, UpdateDelegate> middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }

    public BotApplication Use<TMiddleware>() where TMiddleware : IUpdateMiddleware
    {
        _middlewares.Add(next => async context =>
        {
            TMiddleware middleware = context.RequestServices.GetRequiredService<TMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    public BotApplication UseErrorHandling()
    {
        _middlewares.Add(next => async context =>
        {
            ErrorHandlingMiddleware middleware = context.RequestServices.GetRequiredService<ErrorHandlingMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    public BotApplication UseLogging()
    {
        _middlewares.Add(next => async context =>
        {
            LoggingMiddleware middleware = context.RequestServices.GetRequiredService<LoggingMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    public BotApplication UseSession()
    {
        _middlewares.Add(next => async context =>
        {
            SessionMiddleware middleware = context.RequestServices.GetRequiredService<SessionMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    public BotApplication UseAccessPolicy()
    {
        _middlewares.Add(next => async context =>
        {
            AccessPolicyMiddleware middleware = context.RequestServices.GetRequiredService<AccessPolicyMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    // -- Route registration (Minimal API style with DI) --

    public BotApplication MapCommand(string command, Delegate handler)
    {
        _router.AddRoute(RouteEntry.Command(command, HandlerDelegateFactory.Create(handler)));
        return this;
    }

    public BotApplication MapCallback(string pattern, Delegate handler)
    {
        _router.AddRoute(RouteEntry.Callback(pattern, HandlerDelegateFactory.Create(handler)));
        return this;
    }

    /// <summary>
    /// Регистрирует обработчик action-кнопки. В отличие от MapCallback:
    /// - автоматически отвечает на callback (убирает часики с кнопки)
    /// - если обработчик возвращает <see cref="ScreenView"/>, показывает его
    ///   в nav-сообщении с поддержкой кнопки "← Назад"
    /// </summary>
    public BotApplication MapAction(string callbackId, Delegate handler)
    {
        UpdateDelegate inner = HandlerDelegateFactory.CreateForAction(handler, callbackId);

        _router.AddRoute(RouteEntry.Callback(callbackId, async ctx =>
        {
            IUpdateResponder responder = ctx.RequestServices.GetRequiredService<IUpdateResponder>();
            await responder.AnswerCallbackAsync(ctx);
            await inner(ctx);
        }));

        return this;
    }

    public BotApplication MapCallbackGroup(string prefix, Delegate handler)
    {
        _router.AddRoute(RouteEntry.Callback($"{prefix}:*",
            HandlerDelegateFactory.CreateForCallbackGroup(handler, prefix)));
        return this;
    }

    public BotApplication MapMessage(Func<UpdateContext, bool> predicate, Delegate handler)
    {
        _router.AddRoute(RouteEntry.Message(predicate, HandlerDelegateFactory.Create(handler)));
        return this;
    }

    public BotApplication MapUpdate(Func<UpdateContext, bool> predicate, Delegate handler)
    {
        _router.AddRoute(RouteEntry.Update(predicate, HandlerDelegateFactory.Create(handler)));
        return this;
    }

    public BotApplication MapFallback(Delegate handler)
    {
        _router.SetFallback(HandlerDelegateFactory.Create(handler));
        return this;
    }

    // -- Menu --

    public BotApplication SetMenu(Action<MenuBuilder> configure)
    {
        _menuBuilder = new MenuBuilder();
        configure(_menuBuilder);
        return this;
    }

    // -- Run --

    public async Task RunAsync()
    {
        var pipeline = UpdatePipeline.Build(_middlewares, _router.BuildTerminal());
        var runtime = new BotRuntime(_app);
        await runtime.RunAsync(pipeline, _menuBuilder);
    }
}

internal sealed class PipelineHolder
{
    public UpdatePipeline Pipeline { get; set; } = UpdatePipeline.Build([], _ => Task.CompletedTask);
}
