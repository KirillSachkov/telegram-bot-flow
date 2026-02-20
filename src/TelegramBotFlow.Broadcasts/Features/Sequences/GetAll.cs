using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TelegramBotFlow.Broadcasts.Infrastructure;

namespace TelegramBotFlow.Broadcasts.Features.Sequences;

public sealed record SequenceStepDto(
    Guid Id,
    int Order,
    long FromChatId,
    int MessageId,
    int DelayMinutesAfterJoin);

public sealed record SequenceDto(
    Guid Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    List<SequenceStepDto> Steps);

public sealed class GetAllSequencesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        _ = app.MapGet("/api/sequences", Handler)
            .WithTags("Sequences")
            .WithSummary("Получить все последовательности");
    }

    private static async Task<EndpointResult<List<SequenceDto>>> Handler(
        BroadcastsDbContext db,
        CancellationToken ct)
    {
        List<SequenceDto> sequences = await db.Sequences
            .AsNoTracking()
            .Include(s => s.Steps.OrderBy(st => st.Order))
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SequenceDto(
                s.Id,
                s.Name,
                s.IsActive,
                s.CreatedAt,
                s.Steps.Select(st => new SequenceStepDto(
                        st.Id,
                        st.Order,
                        st.FromChatId,
                        st.MessageId,
                        (int)st.DelayAfterJoin.TotalMinutes))
                    .ToList()))
            .ToListAsync(ct);

        return sequences;
    }
}
