using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TelegramBotFlow.Core.Data;

namespace TelegramBotFlow.Broadcasts.Features.Users;

public sealed record UserDto(
    long TelegramId,
    DateTime JoinedAt,
    bool IsBlocked);

public sealed class GetAllUsersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        _ = app.MapGet("/api/users", Handler)
            .WithTags("Users")
            .WithSummary("Получить всех пользователей бота");
    }

    private static async Task<EndpointResult<List<UserDto>>> Handler(
        BotDbContext db,
        CancellationToken ct)
    {
        List<UserDto> users = await db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.JoinedAt)
            .Select(u => new UserDto(u.TelegramId, u.JoinedAt, u.IsBlocked))
            .ToListAsync(ct);

        return users;
    }
}
