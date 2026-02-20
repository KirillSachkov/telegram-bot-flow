using CSharpFunctionalExtensions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Quartz;
using SharedKernel;
using TelegramBotFlow.Broadcasts.Domain;
using TelegramBotFlow.Broadcasts.Infrastructure;

namespace TelegramBotFlow.Broadcasts.Features.Broadcasts;

public sealed class SendBroadcastEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        _ = app.MapPost("/api/broadcasts/{id:guid}/send", Handler)
            .WithTags("Broadcasts")
            .WithSummary("Отправить рассылку всем пользователям");
    }

    private static async Task<EndpointResult> Handler(
        Guid id,
        BroadcastsDbContext db,
        ISchedulerFactory schedulerFactory,
        CancellationToken ct)
    {
        Broadcast? broadcast = await db.Broadcasts.FindAsync([id], ct);
        if (broadcast is null)
            return UnitResult.Failure(GeneralErrors.NotFound(id, "Рассылка"));

        if (broadcast.Status != BroadcastStatus.Draft)
            return UnitResult.Failure(
                GeneralErrors.InvalidOperation("Рассылка уже отправлена или в процессе отправки"));

        // IScheduler scheduler = await schedulerFactory.GetScheduler(ct);

        // IJobDetail job = JobBuilder.Create<BroadcastExecutionJob>()
        //     .WithIdentity($"broadcast-{id}")
        //     .UsingJobData(BroadcastExecutionJob.BROADCAST_ID_KEY, id.ToString())
        //     .Build();

        // ITrigger trigger = TriggerBuilder.Create()
        //     .WithIdentity($"broadcast-trigger-{id}")
        //     .StartNow()
        //     .Build();

        // await scheduler.ScheduleJob(job, trigger, ct);

        return UnitResult.Success<Error>();
    }
}
