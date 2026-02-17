using FluentAssertions;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Sessions;

public sealed class InMemorySessionStoreTests
{
    private readonly InMemorySessionStore _store = new();

    [Fact]
    public async Task GetOrCreateAsync_CreatesNewSession()
    {
        var session = await _store.GetOrCreateAsync(123);

        session.Should().NotBeNull();
        session.UserId.Should().Be(123);
    }

    [Fact]
    public async Task GetOrCreateAsync_ReturnsSameSessionForSameUser()
    {
        var session1 = await _store.GetOrCreateAsync(123);
        session1.Set("key", "value");

        var session2 = await _store.GetOrCreateAsync(123);

        session2.GetString("key").Should().Be("value");
    }

    [Fact]
    public async Task GetOrCreateAsync_ReturnsDifferentSessionsForDifferentUsers()
    {
        var session1 = await _store.GetOrCreateAsync(123);
        var session2 = await _store.GetOrCreateAsync(456);

        session1.UserId.Should().NotBe(session2.UserId);
    }

    [Fact]
    public async Task GetOrCreateAsync_UpdatesLastActivity()
    {
        var session = await _store.GetOrCreateAsync(123);
        var firstActivity = session.LastActivity;

        await Task.Delay(10);
        await _store.GetOrCreateAsync(123);

        session.LastActivity.Should().BeAfter(firstActivity);
    }
}
