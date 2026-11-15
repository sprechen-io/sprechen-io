using Sprechen.Contracts.ApiClient;
using Xunit;

namespace Sprechen.Contracts.Tests.ApiClient;

public sealed class AsyncCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_WhenSharedRequestFails_EvictsFailureForNextAttempt()
    {
        var cache = new AsyncCache<object>();
        var attempts = 0;

        async Task<object> LoadAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return Interlocked.Increment(ref attempts) == 1
                ? throw new HttpRequestException("transient cutover")
                : new object();
        }

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            cache.GetOrCreateAsync(LoadAsync, CancellationToken.None));
        var recovered = await cache.GetOrCreateAsync(LoadAsync, CancellationToken.None);
        var cached = await cache.GetOrCreateAsync(LoadAsync, CancellationToken.None);

        Assert.Same(recovered, cached);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenConcurrentCallersObserveFailure_RetriesFactoryOnlyOnceAfterEviction()
    {
        var cache = new AsyncCache<object>();
        var firstAttempt = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = 0;

        Task<object> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Interlocked.Increment(ref attempts) == 1
                ? firstAttempt.Task
                : Task.FromResult<object>(new object());
        }

        var first = cache.GetOrCreateAsync(LoadAsync, CancellationToken.None);
        var second = cache.GetOrCreateAsync(LoadAsync, CancellationToken.None);
        firstAttempt.SetException(new HttpRequestException("transient cutover"));

        await Assert.ThrowsAsync<HttpRequestException>(() => first);
        await Assert.ThrowsAsync<HttpRequestException>(() => second);
        var recovered = await cache.GetOrCreateAsync(LoadAsync, CancellationToken.None);

        Assert.NotNull(recovered);
        Assert.Equal(2, attempts);
    }
}
