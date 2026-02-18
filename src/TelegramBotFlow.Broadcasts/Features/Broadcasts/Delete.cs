using CSharpFunctionalExtensions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;
using TelegramBotFlow.Broadcasts.Domain;
using TelegramBotFlow.Broadcasts.Infrastructure;

namespace TelegramBotFlow.Broadcasts.Features.Broadcasts;

public sealed class DeleteBroadcastEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/broadcasts/{id:guid}", Handler)
            .WithTags("Broadcasts")
            .WithSummary("Удалить рассылку");
    }

    private static async Task<EndpointResult> Handler(
        Guid id,
        BroadcastsDbContext db,
        CancellationToken ct)
    {
        Broadcast? broadcast = await db.Broadcasts.FindAsync([id], ct);
        if (broadcast is null)
            return UnitResult.Failure(GeneralErrors.NotFound(id, "Рассылка"));

        if (broadcast.Status == BroadcastStatus.Sending)
            return UnitResult.Failure(
                GeneralErrors.InvalidOperation("Нельзя удалить рассылку в процессе отправки"));

        db.Broadcasts.Remove(broadcast);
        await db.SaveChangesAsync(ct);

        return UnitResult.Success<Error>();
    }
}
