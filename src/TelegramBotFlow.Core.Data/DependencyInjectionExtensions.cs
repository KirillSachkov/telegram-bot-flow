using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Data.Middleware;

namespace TelegramBotFlow.Core.Data;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers BotDbContext with default BotUser model.
    /// Connection string is read from Configuration["ConnectionStrings:Database"].
    /// </summary>
    public static IServiceCollection AddBotCoreData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
                               ?? throw new InvalidOperationException("Connection string 'Database' not found.");

        services.AddDbContext<BotDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Register base type so UserTrackingMiddleware<BotUser> can resolve BotDbContext<BotUser>
        services.AddScoped<BotDbContext<BotUser>>(sp => sp.GetRequiredService<BotDbContext>());

        services.AddTransient<UserTrackingMiddleware>();

        return services;
    }

    /// <summary>
    /// Registers a custom BotDbContext with a custom user type.
    /// Use when you need to extend BotUser with additional properties.
    /// <code>
    /// services.AddBotCoreData&lt;AppUser, AppDbContext&gt;(configuration);
    /// </code>
    /// </summary>
    public static IServiceCollection AddBotCoreData<TUser, TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TUser : BotUser, new()
        where TContext : BotDbContext<TUser>
    {
        var connectionString = configuration.GetConnectionString("Database")
                               ?? throw new InvalidOperationException("Connection string 'Database' not found.");

        services.AddDbContext<TContext>(options =>
            options.UseNpgsql(connectionString));

        // Register base type so modules can resolve BotDbContext<TUser>
        services.AddScoped<BotDbContext<TUser>>(sp => sp.GetRequiredService<TContext>());

        services.AddTransient<UserTrackingMiddleware<TUser>>();
        services.AddTransient<UserTrackingMiddleware>();

        return services;
    }
}
