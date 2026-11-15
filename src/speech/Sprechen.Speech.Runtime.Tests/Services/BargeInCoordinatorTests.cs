
namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class BargeInCoordinatorTests
{
    [Fact]
    public void Token_WhenBargeInOccurs_PreviousTokenIsCancelled()
    {
        using var coordinator = new BargeInCoordinator();

        var first = coordinator.Token;

        coordinator.BargeIn();
        var second = coordinator.Token;

        Assert.True(first.IsCancellationRequested);
        Assert.False(second.IsCancellationRequested);
    }

    [Fact]
    public void Token_WhenDisposed_Throws()
    {
        var coordinator = new BargeInCoordinator();
        coordinator.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = coordinator.Token);
    }

    [Fact]
    public void BargeIn_WhenDisposed_Throws()
    {
        var coordinator = new BargeInCoordinator();
        coordinator.Dispose();

        Assert.Throws<ObjectDisposedException>(() => coordinator.BargeIn());
    }
}


