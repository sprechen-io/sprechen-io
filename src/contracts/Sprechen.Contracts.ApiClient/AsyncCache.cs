namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Teilt einen Ladevorgang zwischen Aufrufern und verwirft fehlgeschlagene Ergebnisse, damit spätere Aufrufe erneut laden können.
/// </summary>
/// <typeparam name="T">Typ des zwischengespeicherten Werts.</typeparam>
internal sealed class AsyncCache<T> where T : class
{
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private Task<T>? _cachedTask;

    public Task<T> GetOrCreateAsync(Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var cachedTask = Volatile.Read(ref _cachedTask);
        if (cachedTask is not null)
        {
            return AwaitAndEvictFailureAsync(cachedTask);
        }

        return GetOrCreateSlowAsync(factory, cancellationToken);
    }

    private async Task<T> GetOrCreateSlowAsync(Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken)
    {
        await _mutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _cachedTask ??= factory(CancellationToken.None);
        }
        finally
        {
            _mutex.Release();
        }

        return await AwaitAndEvictFailureAsync(_cachedTask).ConfigureAwait(false);
    }

    private async Task<T> AwaitAndEvictFailureAsync(Task<T> cachedTask)
    {
        try
        {
            return await cachedTask.ConfigureAwait(false);
        }
        catch
        {
            await _mutex.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                if (ReferenceEquals(_cachedTask, cachedTask))
                {
                    _cachedTask = null;
                }
            }
            finally
            {
                _mutex.Release();
            }

            throw;
        }
    }
}
