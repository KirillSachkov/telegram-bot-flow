using System.Linq.Expressions;
using System.Reflection;
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
    /// Supported return types: <c>Task</c> (void) and <c>Task&lt;IEndpointResult&gt;</c>.
    /// </summary>
    public static UpdateDelegate Create(Delegate handler)
    {
        MethodInfo m = handler.Method;
        ValidateReturnType(m.ReturnType);
        Func<UpdateContext, string?, object?>[] resolvers = BuildResolvers(m.GetParameters());
        object? target = handler.Target;
        Func<object?, object?[], object?> invoker = CompileInvoker(m);

        return async context =>
        {
            object?[] args = ApplyResolvers(resolvers, context, callbackAction: null);
            object? result = invoker(target, args);
            await DispatchAsync(result, context);
        };
    }

    /// <summary>
    /// Creates a delegate for input handlers.
    /// Supported return types: <c>Task</c> (void = auto-back) and <c>Task&lt;IEndpointResult&gt;</c>.
    /// Clears <c>PendingInputActionId</c> before invocation; restores it when result has <c>KeepPending = true</c>.
    /// </summary>
    public static UpdateDelegate CreateForInput(Delegate handler, string actionId)
    {
        MethodInfo m = handler.Method;
        ValidateInputReturnType(m.ReturnType);
        var resolvers = BuildResolvers(m.GetParameters());
        object? target = handler.Target;
        bool returnsEndpointResult = m.ReturnType == _taskEndpointResultType;
        Func<object?, object?[], object?> invoker = CompileInvoker(m);

        return async context =>
        {
            context.Session?.SetPending(null);

            object?[] args = ApplyResolvers(resolvers, context, callbackAction: null);
            object? result = invoker(target, args);

            IEndpointResult er = returnsEndpointResult && result is Task<IEndpointResult> t
                ? await t
                : await WrapVoidAsync(result);

            if (er.KeepPending)
                context.Session?.SetPending(actionId);

            IScreenNavigator navigator = context.RequestServices.GetRequiredService<IScreenNavigator>();
            await er.ExecuteAsync(context, navigator);
        };
    }

    /// <summary>
    /// Creates a delegate for callback-group handlers.
    /// The first <c>string</c> parameter receives the action part after <c>{prefix}:</c>.
    /// </summary>
    public static UpdateDelegate CreateForCallbackGroup(Delegate handler, string prefix)
    {
        MethodInfo m = handler.Method;
        var resolvers = BuildResolvers(m.GetParameters(), hasCallbackAction: true);
        object? target = handler.Target;
        Func<object?, object?[], object?> invoker = CompileInvoker(m);

        return async context =>
        {
            string action = context.CallbackData![(prefix.Length + 1)..];
            object?[] args = ApplyResolvers(resolvers, context, callbackAction: action);
            object? result = invoker(target, args);
            await DispatchAsync(result, context);
        };
    }

    private static async Task DispatchAsync(object? result, UpdateContext context)
    {
        if (result is Task<IEndpointResult> t)
        {
            IEndpointResult er = await t;
            IScreenNavigator navigator = context.RequestServices.GetRequiredService<IScreenNavigator>();
            await er.ExecuteAsync(context, navigator);
        }
        else if (result is Task task)
        {
            await task;
        }
    }

    private static async Task<IEndpointResult> WrapVoidAsync(object? result)
    {
        if (result is Task task)
            await task;
        return new NavigateBackResult();
    }

    private static void ValidateReturnType(Type returnType)
    {
        if (returnType == typeof(void) || returnType == typeof(Task) || returnType == _taskEndpointResultType)
            return;

        throw new InvalidOperationException(
            $"Handler return type '{returnType.Name}' is not supported. " +
            $"Supported types: Task (void), Task<IEndpointResult>.");
    }

    private static void ValidateInputReturnType(Type returnType)
    {
        if (returnType == typeof(void) || returnType == typeof(Task) || returnType == _taskEndpointResultType)
            return;

        throw new InvalidOperationException(
            $"MapInput handler must return Task or Task<IEndpointResult>, got '{returnType.Name}'.");
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
    /// Это ~20-50x быстрее, чем MethodInfo.Invoke на hot path.
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

        Expression body = method.ReturnType == typeof(void)
            ? Expression.Block(call, Expression.Constant(null, typeof(object)))
            : Expression.Convert(call, typeof(object));

        return Expression.Lambda<Func<object?, object?[], object?>>(body, target, args).Compile();
    }
}
