using Sprechen.Contracts.ApiClient;
using Xunit;

namespace Sprechen.Contracts.Tests.ApiClient;

public sealed class ContractsApiResourceCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_WhenKeyIsRepeated_ReusesCachedValue()
    {
        var cache = new ContractsApiResourceCache<object>();
        var factoryCalls = 0;

        var first = await cache.GetOrCreateAsync("same", CreateAsync, CancellationToken.None);
        var second = await cache.GetOrCreateAsync("same", CreateAsync, CancellationToken.None);

        Assert.Same(first, second);
        Assert.Equal(1, factoryCalls);

        Task<object> CreateAsync(CancellationToken _)
        {
            factoryCalls++;
            return Task.FromResult(new object());
        }
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenKeyDiffers_LoadsEachResource()
    {
        var cache = new ContractsApiResourceCache<object>();
        var factoryCalls = 0;

        await cache.GetOrCreateAsync("one", CreateAsync, CancellationToken.None);
        await cache.GetOrCreateAsync("two", CreateAsync, CancellationToken.None);

        Assert.Equal(2, factoryCalls);

        Task<object> CreateAsync(CancellationToken _)
        {
            factoryCalls++;
            return Task.FromResult(new object());
        }
    }
}
