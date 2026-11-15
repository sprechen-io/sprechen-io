using Xunit;
using Sprechen.Service.Deepgram.Services;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class ExponentialBackoffTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NextDelay_WhenDelayIsNonPositive_ReturnsZero(int milliseconds)
    {
        var delay = TimeSpan.FromMilliseconds(milliseconds);

        var result = ExponentialBackoff.NextDelay(delay, TimeSpan.FromSeconds(1));

        Assert.Equal(TimeSpan.Zero, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NextDelay_WhenMaxDelayIsNonPositive_ReturnsDelay(int maxDelayMilliseconds)
    {
        var delay = TimeSpan.FromMilliseconds(250);

        var result = ExponentialBackoff.NextDelay(delay, TimeSpan.FromMilliseconds(maxDelayMilliseconds));

        Assert.Equal(delay, result);
    }

    [Fact]
    public void NextDelay_WhenDelayIsWithinMax_DoublesDelay()
    {
        var delay = TimeSpan.FromMilliseconds(250);
        var maxDelay = TimeSpan.FromSeconds(2);

        var result = ExponentialBackoff.NextDelay(delay, maxDelay);

        Assert.Equal(TimeSpan.FromMilliseconds(500), result);
    }

    [Fact]
    public void NextDelay_WhenDelayExceedsMax_ClampsToMax()
    {
        var delay = TimeSpan.FromSeconds(2);
        var maxDelay = TimeSpan.FromSeconds(3);

        var result = ExponentialBackoff.NextDelay(delay, maxDelay);

        Assert.Equal(maxDelay, result);
    }
}
