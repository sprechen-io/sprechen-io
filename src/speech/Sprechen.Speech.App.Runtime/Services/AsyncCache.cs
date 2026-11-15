namespace Sprechen.Speech.App.Runtime.Services;

/// <summary>
/// Teilt einen asynchron erzeugten Wert zwischen Aufrufern und serialisiert dessen Aufbau, damit gleichzeitige Zugriffe keine parallelen Erzeugungen starten.
/// </summary>
public sealed class AsyncCache<T> where T : class
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private T? _cached;

    public async Task<T> GetOrCreateAsync(Func<CancellationToken, Task<T>> valueFactory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(valueFactory);

        var cached = Volatile.Read(ref _cached);
        if (cached is not null)
        {
            return cached;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cached = _cached;
            if (cached is not null)
            {
                return cached;
            }

            var created = await valueFactory(cancellationToken).ConfigureAwait(false);
            if (created is null)
            {
                throw new InvalidOperationException("AsyncCache value factory returned null.");
            }

            Volatile.Write(ref _cached, created);
            return created;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Reset() => Volatile.Write(ref _cached, null);
}



