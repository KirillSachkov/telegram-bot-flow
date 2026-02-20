using CSharpFunctionalExtensions;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SharedKernel;
using TelegramBotFlow.Broadcasts.Infrastructure;

namespace TelegramBotFlow.Broadcasts.Features.Sequences;

public sealed class DeleteSequenceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        _ = app.MapDelete("/api/sequences/{id:guid}", Handler)
            .WithTags("Sequences")
            .WithSummary("Удалить последовательность");
    }

    private static async Task<EndpointResult> Handler(
        Guid id,
        BroadcastsDbContext db,
        CancellationToken ct)
    {
        Domain.BroadcastSequence? sequence = await db.Sequences.FindAsync([id], ct);
        if (sequence is null)
            return UnitResult.Failure(GeneralErrors.NotFound(id, "Последовательность"));

        _ = db.Sequences.Remove(sequence);
        _ = await db.SaveChangesAsync(ct);

        return UnitResult.Success<Error>();
    }
}
