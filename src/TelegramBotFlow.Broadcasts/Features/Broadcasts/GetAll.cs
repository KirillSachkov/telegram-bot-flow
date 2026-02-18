using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TelegramBotFlow.Broadcasts.Infrastructure;

namespace TelegramBotFlow.Broadcasts.Features.Broadcasts;

public sealed record BroadcastDto(
    Guid Id,
    long FromChatId,
    int MessageId,
    string Status,
    DateTime CreatedAt,
    DateTime? SentAt,
    int SuccessCount,
    int FailureCount);

public sealed class GetAllBroadcastsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/broadcasts", Handler)
            .WithTags("Broadcasts")
            .WithSummary("Получить все рассылки");
    }

    private static async Task<EndpointResult<List<BroadcastDto>>> Handler(
        BroadcastsDbContext db,
        CancellationToken ct)
    {
        List<BroadcastDto> broadcasts = await db.Broadcasts
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BroadcastDto(
                b.Id,
                b.FromChatId,
                b.MessageId,
                b.Status.ToString(),
                b.CreatedAt,
                b.SentAt,
                b.SuccessCount,
                b.FailureCount))
            .ToListAsync(ct);

        return broadcasts;
    }
}
