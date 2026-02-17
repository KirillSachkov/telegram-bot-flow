using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Extensions;
using TelegramBotFlow.Core.Flows;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.Core.Hosting;

public sealed class BotApplication
{
    private readonly WebApplication _app;
    private readonly UpdateRouter _router;
    private readonly FlowManager _flowManager;
    private readonly List<Func<UpdateDelegate, UpdateDelegate>> _middlewares = [];
    private MenuBuilder? _menuBuilder;

    public IServiceProvider Services => _app.Services;

    private BotApplication(WebApplication app, UpdateRouter router, FlowManager flowManager)
    {
        _app = app;
        _router = router;
        _flowManager = flowManager;
    }

    public static BotApplicationBuilder CreateBuilder(string[] args) => new(args);

    public static BotApplication Build(BotApplicationBuilder builder)
    {
        builder.Services.AddTelegramBotFlow(builder.Configuration);

        var app = builder.WebAppBuilder.Build();

        var router = app.Services.GetRequiredService<UpdateRouter>();
        var flowManager = app.Services.GetRequiredService<FlowManager>();

        return new BotApplication(app, router, flowManager);
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
            var middleware = context.Services.GetRequiredService<TMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    public BotApplication UseErrorHandling()
    {
        _middlewares.Add(next => async context =>
        {
            var middleware = context.Services.GetRequiredService<ErrorHandlingMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    public BotApplication UseLogging()
    {
        _middlewares.Add(next => async context =>
        {
            var middleware = context.Services.GetRequiredService<LoggingMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    public BotApplication UseSession()
    {
        _middlewares.Add(next => async context =>
        {
            var middleware = context.Services.GetRequiredService<SessionMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    public BotApplication UseFlows()
    {
        _middlewares.Add(next => async context =>
        {
            var middleware = context.Services.GetRequiredService<FlowMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    // -- Route registration (Minimal API style) --

    public BotApplication MapCommand(string command, Func<UpdateContext, Task> handler)
    {
        _router.AddRoute(RouteEntry.Command(command, ctx => handler(ctx)));
        return this;
    }

    public BotApplication MapCallback(string pattern, Func<UpdateContext, Task> handler)
    {
        _router.AddRoute(RouteEntry.Callback(pattern, ctx => handler(ctx)));
        return this;
    }

    public BotApplication MapCallbackGroup(string prefix, Func<UpdateContext, string, Task> handler)
    {
        _router.AddRoute(RouteEntry.Callback($"{prefix}:*", async ctx =>
        {
            var action = ctx.CallbackData![(prefix.Length + 1)..];
            await handler(ctx, action);
        }));
        return this;
    }

    public BotApplication MapMessage(Func<UpdateContext, bool> predicate, Func<UpdateContext, Task> handler)
    {
        _router.AddRoute(RouteEntry.Message(predicate, ctx => handler(ctx)));
        return this;
    }

    public BotApplication MapUpdate(Func<UpdateContext, bool> predicate, Func<UpdateContext, Task> handler)
    {
        _router.AddRoute(RouteEntry.Update(predicate, ctx => handler(ctx)));
        return this;
    }

    public BotApplication MapButton(string screen, string buttonText, Func<UpdateContext, Task> handler)
    {
        _router.AddRoute(RouteEntry.Message(
            ctx => ctx.Screen == screen && ctx.MessageText == buttonText,
            ctx => handler(ctx)));
        return this;
    }

    public BotApplication MapFlow(string command, Action<FlowBuilder> configure)
    {
        var flowBuilder = new FlowBuilder(command);
        configure(flowBuilder);

        var flow = flowBuilder.Build();
        _flowManager.Register(flow);

        _router.AddRoute(RouteEntry.Command(command,
            async ctx => { await _flowManager.StartFlowAsync(ctx, flow.Id); }));

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

        var services = _app.Services;
        ReplaceUpdatePipeline(services, pipeline);

        var config = services.GetRequiredService<IOptions<BotConfiguration>>().Value;

        if (config.Mode == BotMode.Webhook)
        {
            _app.MapPost(config.WebhookPath, async (
                Update update,
                ITelegramBotClient bot,
                IServiceProvider sp,
                CancellationToken ct) =>
            {
                await WebhookEndpoints.HandleWebhookUpdate(update, bot, pipeline, sp, ct);
                return Results.Ok();
            });

            var bot = services.GetRequiredService<ITelegramBotClient>();
            await bot.SetWebhook(config.WebhookUrl + config.WebhookPath, allowedUpdates: []);
        }

        if (_menuBuilder is not null)
        {
            var menuBot = services.GetRequiredService<ITelegramBotClient>();
            await _menuBuilder.ApplyAsync(menuBot);
        }

        await _app.RunAsync();
    }

    private static void ReplaceUpdatePipeline(IServiceProvider services, UpdatePipeline pipeline)
    {
        var holder = services.GetRequiredService<PipelineHolder>();
        holder.Pipeline = pipeline;
    }
}

internal sealed class PipelineHolder
{
    public UpdatePipeline Pipeline { get; set; } = UpdatePipeline.Build([], _ => Task.CompletedTask);
}
