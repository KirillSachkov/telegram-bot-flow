using FluentAssertions;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Sessions;

public sealed class InMemorySessionStoreTests
{
    private readonly InMemorySessionStore _store = new();

    [Fact]
    public async Task GetOrCreateAsync_CreatesNewSession()
    {
        UserSession session = await _store.GetOrCreateAsync(123);

        _ = session.Should().NotBeNull();
        _ = session.UserId.Should().Be(123);
    }

    [Fact]
    public async Task GetOrCreateAsync_ReturnsSameSessionForSameUser()
    {
        UserSession session1 = await _store.GetOrCreateAsync(123);
        session1.Set("key", "value");

        UserSession session2 = await _store.GetOrCreateAsync(123);

        _ = session2.GetString("key").Should().Be("value");
    }

    [Fact]
    public async Task GetOrCreateAsync_ReturnsDifferentSessionsForDifferentUsers()
    {
        UserSession session1 = await _store.GetOrCreateAsync(123);
        UserSession session2 = await _store.GetOrCreateAsync(456);

        _ = session1.UserId.Should().NotBe(session2.UserId);
    }

    [Fact]
    public async Task GetOrCreateAsync_UpdatesLastActivity()
    {
        UserSession session = await _store.GetOrCreateAsync(123);
        DateTime firstActivity = session.LastActivity;

        await Task.Delay(10);
        _ = await _store.GetOrCreateAsync(123);

        _ = session.LastActivity.Should().BeAfter(firstActivity);
    }
}
