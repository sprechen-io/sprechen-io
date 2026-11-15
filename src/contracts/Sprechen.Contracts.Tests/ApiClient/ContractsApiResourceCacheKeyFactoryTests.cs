using Sprechen.Contracts.ApiClient;
using Xunit;

namespace Sprechen.Contracts.Tests.ApiClient;

public sealed class ContractsApiResourceCacheKeyFactoryTests
{
    [Fact]
    public void Create_EncodesSegmentsToPreventSlashCollisions()
    {
        var factory = new ContractsApiResourceCacheKeyFactory();

        var first = factory.Create("a/b", "c");
        var second = factory.Create("a", "b/c");

        Assert.Equal("a%2Fb/c", first);
        Assert.Equal("a/b%2Fc", second);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Create_RejectsBlankSegments()
    {
        var factory = new ContractsApiResourceCacheKeyFactory();

        Assert.Throws<ArgumentException>(() => factory.Create("application", " "));
    }
}
