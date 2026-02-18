using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace TelegramBotFlow.Core.Screens;

public sealed class ScreenRegistry
{
    private readonly Dictionary<string, Type> _screens = [];

    public void Register<TScreen>() where TScreen : class, IScreen =>
        Register(typeof(TScreen));

    public void Register(Type screenType)
    {
        if (!typeof(IScreen).IsAssignableFrom(screenType))
            throw new ArgumentException($"Type {screenType.Name} does not implement IScreen.");

        _screens[GetIdFromType(screenType)] = screenType;
    }

    public void RegisterWithId(string screenId, Type screenType)
    {
        if (!typeof(IScreen).IsAssignableFrom(screenType))
            throw new ArgumentException($"Type {screenType.Name} does not implement IScreen.");

        _screens[screenId] = screenType;
    }

    public IScreen Resolve(string screenId, IServiceProvider services)
    {
        if (!_screens.TryGetValue(screenId, out Type? screenType))
            throw new InvalidOperationException($"Screen '{screenId}' is not registered.");

        return (IScreen)services.GetRequiredService(screenType);
    }

    public bool HasScreen(string screenId) => _screens.ContainsKey(screenId);

    public IReadOnlyCollection<string> GetRegisteredIds() => _screens.Keys;

    /// <summary>
    /// Returns the screen ID that would be assigned to the given screen type by convention.
    /// Convention: strip "Screen" suffix, convert PascalCase to snake_case.
    /// Examples: MainMenuScreen → main_menu, ProfileScreen → profile.
    /// </summary>
    public static string GetIdFor<TScreen>() where TScreen : class, IScreen =>
        GetIdFromType(typeof(TScreen));

    /// <inheritdoc cref="GetIdFor{TScreen}"/>
    public static string GetIdFromType(Type screenType)
    {
        string name = screenType.Name;

        if (name.EndsWith("Screen", StringComparison.Ordinal))
            name = name[..^"Screen".Length];

        return ToSnakeCase(name);
    }

    internal void RegisterFromAssembly(Assembly assembly)
    {
        foreach (Type screenType in GetScreenTypes(assembly))
            Register(screenType);
    }

    internal static IEnumerable<Type> GetScreenTypes(Assembly assembly) =>
        assembly.DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && t.IsAssignableTo(typeof(IScreen)));

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder(input.Length + 4);
        for (int i = 0; i < input.Length; i++)
        {
            if (i > 0 && char.IsUpper(input[i]))
                sb.Append('_');
            sb.Append(char.ToLower(input[i]));
        }
        return sb.ToString();
    }
}
