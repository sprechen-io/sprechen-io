using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Sprechen.Service.Core.Services;
using Xunit;

namespace Sprechen.Service.Core.Tests;

public sealed class SpeechProviderHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenDeepgramPortsAreConstructed_ReturnsHealthy()
    {
        var sut = new SpeechProviderHealthCheck(
            Mock.Of<ISpeechToTextProvider>(MockBehavior.Strict),
            Mock.Of<ITextToSpeechProvider>(MockBehavior.Strict),
            Microsoft.Extensions.Options.Options.Create(new SpeechProviderOptions()));

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Deepgram", result.Data["sttProvider"]);
        Assert.Equal("Deepgram", result.Data["ttsProvider"]);
        Assert.Contains("ISpeechToTextProvider", Assert.IsType<string>(result.Data["sttPort"]));
        Assert.Contains("ITextToSpeechProvider", Assert.IsType<string>(result.Data["ttsPort"]));
        Assert.Equal("STT Deepgram, TTS Deepgram.", result.Description);
    }
}
