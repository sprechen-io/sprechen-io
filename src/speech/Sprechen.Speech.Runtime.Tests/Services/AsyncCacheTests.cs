using System.Collections.Concurrent;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class AsyncCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_WhenCalledMultipleTimes_CachesSuccessfulResult()
    {
        var cache = new AsyncCache<object>();
        var callCount = 0;

        Task<object> Factory(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref callCount);
            return Task.FromResult<object>(new object());
        }

        var first = await cache.GetOrCreateAsync(Factory, CancellationToken.None);
        var second = await cache.GetOrCreateAsync(Factory, CancellationToken.None);

        Assert.Same(first, second);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenFactoryThrows_DoesNotCacheFailure()
    {
        var cache = new AsyncCache<object>();
        var callCount = 0;

        Task<object> Factory(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref callCount);
            throw new InvalidOperationException("boom");
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetOrCreateAsync(Factory, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetOrCreateAsync(Factory, CancellationToken.None));

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenConcurrent_CallsFactoryOnceAndReturnsSameInstance()
    {
        var cache = new AsyncCache<object>();
        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;

        async Task<object> Factory(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref callCount);
            return await tcs.Task.WaitAsync(cancellationToken);
        }

        var firstTask = cache.GetOrCreateAsync(Factory, CancellationToken.None);
        var secondTask = cache.GetOrCreateAsync(Factory, CancellationToken.None);

        var instance = new object();
        tcs.SetResult(instance);

        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(1, callCount);
        Assert.Same(instance, results[0]);
        Assert.Same(instance, results[1]);
    }

    [Fact]
    public async Task Reset_WhenCalled_AllowsRecreatingTheValue()
    {
        var cache = new AsyncCache<object>();
        var instances = new ConcurrentQueue<object>();

        Task<object> Factory(CancellationToken cancellationToken)
        {
            var value = new object();
            instances.Enqueue(value);
            return Task.FromResult(value);
        }

        var first = await cache.GetOrCreateAsync(Factory, CancellationToken.None);
        cache.Reset();
        var second = await cache.GetOrCreateAsync(Factory, CancellationToken.None);

        Assert.NotSame(first, second);
        Assert.Equal(2, instances.Count);
    }
}



