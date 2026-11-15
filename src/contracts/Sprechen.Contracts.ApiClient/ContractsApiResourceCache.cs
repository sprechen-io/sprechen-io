using System.Collections.Concurrent;

namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Teilt pro Schlüssel einen Cache, damit parallele Aufrufer dasselbe Vertragsdokument nur einmal laden.
/// </summary>
/// <typeparam name="T">Typ des zwischengespeicherten Vertragsdokuments.</typeparam>
internal sealed class ContractsApiResourceCache<T> where T : class
{
    private readonly ConcurrentDictionary<string, AsyncCache<T>> _entries = new(StringComparer.Ordinal);

    public Task<T> GetOrCreateAsync(
        string cacheKey,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
        ArgumentNullException.ThrowIfNull(factory);

        var cache = _entries.GetOrAdd(cacheKey, _ => new AsyncCache<T>());
        return cache.GetOrCreateAsync(factory, cancellationToken);
    }
}
