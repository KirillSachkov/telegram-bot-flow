using System.Reflection;
using Framework.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using TelegramBotFlow.Broadcasts.Infrastructure;
using TelegramBotFlow.Broadcasts.Services;
using TelegramBotFlow.Core.Endpoints;

namespace TelegramBotFlow.Broadcasts;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddBroadcasts(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Database")
                                  ?? throw new InvalidOperationException("Connection string 'Database' not found.");

        _ = services.AddDbContext<BroadcastsDbContext>(options =>
            options.UseNpgsql(connectionString));

        _ = services.AddScoped<BroadcastSender>();

        _ = services.AddEndpoints(Assembly.GetExecutingAssembly());

        _ = services.AddBotEndpoints(Assembly.GetExecutingAssembly());

        _ = services.AddQuartz(q =>
        {
            q.UsePersistentStore(store =>
            {
                store.UseProperties = true;
                store.UsePostgres(connectionString);
                store.UseSystemTextJsonSerializer();
            });

            // TODO: Uncomment when Quartz tables are migrated to PostgreSQL
            // var sequenceJobKey = new JobKey("SequenceProcessorJob");
            // q.AddJob<SequenceProcessorJob>(opts => opts
            //     .WithIdentity(sequenceJobKey)
            //     .StoreDurably());
            //
            // q.AddTrigger(opts => opts
            //     .ForJob(sequenceJobKey)
            //     .WithIdentity("SequenceProcessorTrigger")
            //     .WithSimpleSchedule(x => x
            //         .WithIntervalInMinutes(1)
            //         .RepeatForever()));
        });

        _ = services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }
}
