using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Routing;

public static class HandlerDelegateFactory
{
    public static UpdateDelegate Create(Delegate handler)
    {
        MethodInfo method = handler.Method;
        ParameterInfo[] parameters = method.GetParameters();
        object? target = handler.Target;

        return async context =>
        {
            object?[] args = ResolveArguments(parameters, context, callbackAction: null);
            object? result = method.Invoke(target, args);

            if (result is Task task)
                await task;
        };
    }

    public static UpdateDelegate CreateForAction(Delegate handler, string callbackId)
    {
        MethodInfo method = handler.Method;
        ParameterInfo[] parameters = method.GetParameters();
        object? target = handler.Target;

        return async context =>
        {
            object?[] args = ResolveArguments(parameters, context, callbackAction: null);
            object? result = method.Invoke(target, args);

            if (result is Task<ScreenView> screenViewTask)
            {
                ScreenView view = await screenViewTask;
                IScreenNavigator navigator = context.RequestServices.GetRequiredService<IScreenNavigator>();
                await navigator.ShowViewAsync(context, view);
            }
            else if (result is Task task)
            {
                await task;
            }
        };
    }

    public static UpdateDelegate CreateForCallbackGroup(Delegate handler, string prefix)
    {
        MethodInfo method = handler.Method;
        ParameterInfo[] parameters = method.GetParameters();
        object? target = handler.Target;

        return async context =>
        {
            string action = context.CallbackData![(prefix.Length + 1)..];
            object?[] args = ResolveArguments(parameters, context, callbackAction: action);
            object? result = method.Invoke(target, args);

            if (result is Task task)
                await task;
        };
    }

    private static object?[] ResolveArguments(
        ParameterInfo[] parameters,
        UpdateContext context,
        string? callbackAction)
    {
        object?[] args = new object?[parameters.Length];
        bool actionConsumed = false;

        for (int i = 0; i < parameters.Length; i++)
        {
            Type paramType = parameters[i].ParameterType;

            if (paramType == typeof(UpdateContext))
            {
                args[i] = context;
            }
            else if (paramType == typeof(CancellationToken))
            {
                args[i] = context.CancellationToken;
            }
            else if (paramType == typeof(string) && callbackAction is not null && !actionConsumed)
            {
                args[i] = callbackAction;
                actionConsumed = true;
            }
            else
            {
                args[i] = context.RequestServices.GetRequiredService(paramType);
            }
        }

        return args;
    }
}
