using System.Collections.Concurrent;
namespace TelegramBotFlow.Core.Wizards;

/// <summary>
/// Реализация хранилища состояний визардов в памяти. Подходит для разработки.
/// В продакшене рекомендуется использовать Redis или БД.
/// </summary>
internal sealed class InMemoryWizardStore : IWizardStore
{
    private readonly ConcurrentDictionary<string, WizardStorageState> _store = new();

    public Task<WizardStorageState?> GetAsync(long userId, string wizardId,
        CancellationToken cancellationToken = default)
    {
        string key = GetKey(userId, wizardId);

        if (_store.TryGetValue(key, out WizardStorageState? state))
        {
            if (state.ExpiresAt.HasValue && state.ExpiresAt.Value < DateTime.UtcNow)
            {
                _store.TryRemove(key, out _);
                return Task.FromResult<WizardStorageState?>(null);
            }

            return Task.FromResult<WizardStorageState?>(state);
        }

        return Task.FromResult<WizardStorageState?>(null);
    }

    public Task SaveAsync(long userId, string wizardId, WizardStorageState state,
        CancellationToken cancellationToken = default)
    {
        string key = GetKey(userId, wizardId);
        _store[key] = state;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(long userId, string wizardId, CancellationToken cancellationToken = default)
    {
        string key = GetKey(userId, wizardId);
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private static string GetKey(long userId, string wizardId) => $"{userId}:{wizardId}";
}