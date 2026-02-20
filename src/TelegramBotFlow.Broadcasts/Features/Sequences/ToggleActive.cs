using CSharpFunctionalExtensions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;
using TelegramBotFlow.Broadcasts.Infrastructure;

namespace TelegramBotFlow.Broadcasts.Features.Sequences;

public sealed class ToggleSequenceActiveEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        _ = app.MapPatch("/api/sequences/{id:guid}/toggle", Handler)
            .WithTags("Sequences")
            .WithSummary("Включить/выключить последовательность");
    }

    private static async Task<EndpointResult> Handler(
        Guid id,
        BroadcastsDbContext db,
        CancellationToken ct)
    {
        Domain.BroadcastSequence? sequence = await db.Sequences.FindAsync([id], ct);
        if (sequence is null)
            return UnitResult.Failure(GeneralErrors.NotFound(id, "Последовательность"));

        sequence.IsActive = !sequence.IsActive;
        _ = await db.SaveChangesAsync(ct);

        return UnitResult.Success<Error>();
    }
}
