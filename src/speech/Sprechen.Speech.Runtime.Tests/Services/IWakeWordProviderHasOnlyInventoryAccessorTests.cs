using Sprechen.Speech.App.Contracts.Services;
using Xunit;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Begrenzt die Methoden von <see cref="IWakeWordProvider"/> auf den Inventarzugriff,
/// damit der Vertrag keine weiteren Aufgaben übernimmt.
/// </summary>
public sealed class IWakeWordProviderHasOnlyInventoryAccessorTests
{
    [Fact]
    public void IWakeWordProvider_DeclaresExactlyTheInventoryAccessor()
    {
        var methods = typeof(IWakeWordProvider).GetMethods();

        Assert.Single(methods);
        var method = methods[0];
        Assert.Equal("GetWakeWordInventoryAsync", method.Name);
        Assert.Equal(typeof(Task<WakeWordInventory>), method.ReturnType);
    }
}
