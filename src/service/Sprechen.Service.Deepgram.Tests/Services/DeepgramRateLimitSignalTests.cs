using Sprechen.Service.Deepgram.Services;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class DeepgramRateLimitSignalTests
{
    [Theory]
    [InlineData("429", null, null)]
    [InlineData(null, "rate limit exceeded", null)]
    [InlineData(null, null, "Too Many Requests")]
    [InlineData("TOO_MANY_REQUESTS", null, null)]
    [InlineData(null, null, "websocket closed with status code '429'")]
    public void Matches_WithRateLimitSignal_ReturnsTrue(string? errCode, string? errMsg, string? message)
    {
        Assert.True(DeepgramRateLimitSignal.Matches(errCode, errMsg, message));
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("x", null, null)]
    [InlineData("", " ", "")]
    [InlineData("INTERNAL", "server error", "boom")]
    [InlineData(null, null, "transcription failed")]
    public void Matches_WithoutRateLimitSignal_ReturnsFalse(string? errCode, string? errMsg, string? message)
    {
        Assert.False(DeepgramRateLimitSignal.Matches(errCode, errMsg, message));
    }
}
