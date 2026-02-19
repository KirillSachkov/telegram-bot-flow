using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Routing;

public static class HandlerDelegateFactory
{
    private static readonly Type _taskEndpointResultType = typeof(Task<IEndpointResult>);

    /// <summary>
    /// Creates a delegate for general-purpose handlers.
    /// Required return type: <c>Task&lt;IEndpointResult&gt;</c>.
    /// </summary>
    public static UpdateDelegate Create(Delegate handler)
    {
        MethodInfo m = handler.Method;
        ValidateReturnType(m.ReturnType);
        Func<UpdateContext, string?, object?>[] resolvers = BuildResolvers(m.GetParameters());
        object? target = handler.Target;
        Func<object?, object?[], object?> invoker = CompileInvoker(m);

        return context => InvokeAndDispatchAsync(invoker, target, resolvers, context, callbackAction: null);
    }

    /// <summary>
    /// Creates a delegate for input handlers.
    /// Required return type: <c>Task&lt;IEndpointResult&gt;</c>.
    /// Clears <c>PendingInputActionId</c> before invocation; restores it when result has <c>KeepPending = true</c>.
    /// </summary>
    public static UpdateDelegate CreateForInput(Delegate handler, string actionId)
    {
        MethodInfo m = handler.Method;
        ValidateReturnType(m.ReturnType);
        var resolvers = BuildResolvers(m.GetParameters());
        object? target = handler.Target;
        Func<object?, object?[], object?> invoker = CompileInvoker(m);

        return async context =>
        {
            context.Session?.SetPending(null);

            object?[] args = ApplyResolvers(resolvers, context, callbackAction: null);
            IEndpointResult er = await (Task<IEndpointResult>)invoker(target, args)!;

            if (er.KeepPending)
                context.Session?.SetPending(actionId);

            IScreenNavigator navigator = context.RequestServices.GetRequiredService<IScreenNavigator>();
            await er.ExecuteAsync(context, navigator);
        };
    }

    /// <summary>
    /// Creates a delegate for callback-group handlers.
    /// Required return type: <c>Task&lt;IEndpointResult&gt;</c>.
    /// The first <c>string</c> parameter receives the action part after <c>{prefix}:</c>.
    /// </summary>
    public static UpdateDelegate CreateForCallbackGroup(Delegate handler, string prefix)
    {
        MethodInfo m = handler.Method;
        ValidateReturnType(m.ReturnType);
        var resolvers = BuildResolvers(m.GetParameters(), hasCallbackAction: true);
        object? target = handler.Target;
        Func<object?, object?[], object?> invoker = CompileInvoker(m);

        return context =>
        {
            string action = context.CallbackData![(prefix.Length + 1)..];
            return InvokeAndDispatchAsync(invoker, target, resolvers, context, callbackAction: action);
        };
    }

    /// <summary>
    /// Creates a delegate for action handlers expecting a typed payload.
    /// </summary>
    public static UpdateDelegate CreateForActionWithPayload<TPayload>(Delegate handler, string prefix)
    {
        MethodInfo m = handler.Method;
        ValidateReturnType(m.ReturnType);

        bool payloadConsumed = false;
        ParameterInfo[] parameters = m.GetParameters();
        var resolvers = new Func<UpdateContext, string, object?>[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            Type paramType = parameters[i].ParameterType;

            if (paramType == typeof(UpdateContext))
            {
                resolvers[i] = static (ctx, _) => ctx;
            }
            else if (paramType == typeof(CancellationToken))
            {
                resolvers[i] = static (ctx, _) => ctx.CancellationToken;
            }
            else if (paramType == typeof(TPayload) && !payloadConsumed)
            {
                payloadConsumed = true;
                resolvers[i] = static (ctx, payloadData) =>
                {
                    if (payloadData.StartsWith("j:"))
                    {
                        string json = payloadData[2..];
                        return JsonSerializer.Deserialize<TPayload>(json)!;
                    }
                    else if (payloadData.StartsWith("s:"))
                    {
                        string shortId = payloadData[2..];
                        if (ctx.Session is null) throw new Exceptions.PayloadExpiredException();
                        return ctx.Session.GetPayload<TPayload>(shortId);
                    }

                    throw new InvalidOperationException($"Invalid payload format: {payloadData}");
                };
            }
            else
            {
                Type serviceType = paramType;
                resolvers[i] = (ctx, _) => ctx.RequestServices.GetRequiredService(serviceType);
            }
        }

        object? target = handler.Target;
        Func<object?, object?[], object?> invoker = CompileInvoker(m);

        return context =>
        {
            string payloadData = context.CallbackData![(prefix.Length + 1)..];
            return InvokeAndDispatchWithPayloadAsync(invoker, target, resolvers, context, payloadData);
        };
    }

    private static async Task InvokeAndDispatchWithPayloadAsync(
        Func<object?, object?[], object?> invoker,
        object? target,
        Func<UpdateContext, string, object?>[] resolvers,
        UpdateContext context,
        string payloadData)
    {
        object?[] args;
        try
        {
            args = new object?[resolvers.Length];
            for (int i = 0; i < resolvers.Length; i++)
                args[i] = resolvers[i](context, payloadData);
        }
        catch (Exceptions.PayloadExpiredException ex)
        {
            var responder = context.RequestServices.GetRequiredService<IUpdateResponder>();
            await responder.AnswerCallbackAsync(context, ex.Message, showAlert: true);
            return;
        }

        IEndpointResult er = await (Task<IEndpointResult>)invoker(target, args)!;
        IScreenNavigator navigator = context.RequestServices.GetRequiredService<IScreenNavigator>();
        await er.ExecuteAsync(context, navigator);
    }

    private static async Task InvokeAndDispatchAsync(
        Func<object?, object?[], object?> invoker,
        object? target,
        Func<UpdateContext, string?, object?>[] resolvers,
        UpdateContext context,
        string? callbackAction)
    {
        object?[] args = ApplyResolvers(resolvers, context, callbackAction);
        IEndpointResult er = await (Task<IEndpointResult>)invoker(target, args)!;
        IScreenNavigator navigator = context.RequestServices.GetRequiredService<IScreenNavigator>();
        await er.ExecuteAsync(context, navigator);
    }

    private static void ValidateReturnType(Type returnType)
    {
        if (returnType == _taskEndpointResultType)
            return;

        throw new InvalidOperationException(
            $"Handler return type '{returnType.Name}' is not supported. " +
            $"Required: Task<IEndpointResult>.");
    }

    /// <summary>
    /// Pre-builds one resolver function per parameter at registration time.
    /// On each request only a simple array pass is needed — no type-checking per call.
    /// </summary>
    private static Func<UpdateContext, string?, object?>[] BuildResolvers(
        ParameterInfo[] parameters,
        bool hasCallbackAction = false)
    {
        bool callbackConsumed = false;
        var resolvers = new Func<UpdateContext, string?, object?>[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            Type paramType = parameters[i].ParameterType;

            if (paramType == typeof(UpdateContext))
            {
                resolvers[i] = static (ctx, _) => ctx;
            }
            else if (paramType == typeof(CancellationToken))
            {
                resolvers[i] = static (ctx, _) => ctx.CancellationToken;
            }
            else if (hasCallbackAction && paramType == typeof(string) && !callbackConsumed)
            {
                callbackConsumed = true;
                resolvers[i] = static (_, action) => action;
            }
            else
            {
                // Capture the exact service type to avoid closure over loop variable
                Type serviceType = paramType;
                resolvers[i] = (ctx, _) => ctx.RequestServices.GetRequiredService(serviceType);
            }
        }

        return resolvers;
    }

    private static object?[] ApplyResolvers(
        Func<UpdateContext, string?, object?>[] resolvers,
        UpdateContext context,
        string? callbackAction)
    {
        object?[] args = new object?[resolvers.Length];
        for (int i = 0; i < resolvers.Length; i++)
            args[i] = resolvers[i](context, callbackAction);
        return args;
    }

    /// <summary>
    /// Компилирует MethodInfo в типизированный делегат через Expression Tree.
    /// Вызывается один раз при регистрации маршрута, а не на каждый update.
    /// </summary>
    private static Func<object?, object?[], object?> CompileInvoker(MethodInfo method)
    {
        ParameterExpression target = Expression.Parameter(typeof(object), "target");
        ParameterExpression args = Expression.Parameter(typeof(object[]), "args");

        ParameterInfo[] parameters = method.GetParameters();
        Expression[] callArgs = new Expression[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            callArgs[i] = Expression.Convert(
                Expression.ArrayIndex(args, Expression.Constant(i)),
                parameters[i].ParameterType);
        }

        Expression call = method.IsStatic
            ? Expression.Call(method, callArgs)
            : Expression.Call(Expression.Convert(target, method.DeclaringType!), method, callArgs);

        Expression body = Expression.Convert(call, typeof(object));

        return Expression.Lambda<Func<object?, object?[], object?>>(body, target, args).Compile();
    }
}
