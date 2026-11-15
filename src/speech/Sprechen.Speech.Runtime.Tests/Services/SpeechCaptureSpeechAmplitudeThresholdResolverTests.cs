using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechCaptureSpeechAmplitudeThresholdResolverTests
{
    [Fact]
    public async Task UnsetSetting_UsesTheModeledThreshold()
    {
        var settings = new Mock<ISpeechCaptureAmplitudeThresholdSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSpeechAmplitudeThresholdAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(0));
        var resolver = new SpeechCaptureSpeechAmplitudeThresholdResolver(
            settings.Object,
            NullLogger<SpeechCaptureSpeechAmplitudeThresholdResolver>.Instance);

        var result = await resolver.GetSpeechAmplitudeThresholdAsync(CancellationToken.None);

        var modeled = SpeechCaptureProfileCatalogReader.GetSegmenterProfile()!.SpeechAmplitudeThreshold;
        Assert.Equal(modeled, result);
    }

    [Fact]
    public async Task SettingAboveTheNoiseFloor_IsKept()
    {
        var settings = new Mock<ISpeechCaptureAmplitudeThresholdSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSpeechAmplitudeThresholdAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(200));
        var resolver = new SpeechCaptureSpeechAmplitudeThresholdResolver(
            settings.Object,
            NullLogger<SpeechCaptureSpeechAmplitudeThresholdResolver>.Instance);

        var result = await resolver.GetSpeechAmplitudeThresholdAsync(CancellationToken.None);

        Assert.Equal(200, result);
    }

    [Fact]
    public async Task SettingBelowTheNoiseFloor_IsRaised()
    {
        var settings = new Mock<ISpeechCaptureAmplitudeThresholdSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSpeechAmplitudeThresholdAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(10));
        var resolver = new SpeechCaptureSpeechAmplitudeThresholdResolver(
            settings.Object,
            NullLogger<SpeechCaptureSpeechAmplitudeThresholdResolver>.Instance);

        var result = await resolver.GetSpeechAmplitudeThresholdAsync(CancellationToken.None);

        Assert.Equal(SpeechCaptureSpeechAmplitudeThresholdResolver.Minimum, result);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ThrownCancellation_IsGovernedByCallerTokenState(bool callerCancelled, bool exceptionUsesCallerToken)
    {
        using var caller = new CancellationTokenSource();
        using var foreign = new CancellationTokenSource();
        foreign.Cancel();
        if (callerCancelled)
        {
            caller.Cancel();
        }

        var failure = new OperationCanceledException(exceptionUsesCallerToken ? caller.Token : foreign.Token);
        var settings = new Mock<ISpeechCaptureAmplitudeThresholdSettings>(MockBehavior.Strict);
        settings.Setup(owner => owner.GetSpeechAmplitudeThresholdAsync(caller.Token)).ThrowsAsync(failure);
        var resolver = new SpeechCaptureSpeechAmplitudeThresholdResolver(settings.Object, NullLogger<SpeechCaptureSpeechAmplitudeThresholdResolver>.Instance);

        if (callerCancelled)
        {
            var actual = await Assert.ThrowsAsync<OperationCanceledException>(() => resolver.GetSpeechAmplitudeThresholdAsync(caller.Token));
            Assert.Same(failure, actual);
        }
        else
        {
            Assert.Equal(SpeechCaptureProfileCatalogReader.GetSegmenterProfile()!.SpeechAmplitudeThreshold, await resolver.GetSpeechAmplitudeThresholdAsync(caller.Token));
        }

        settings.Verify(owner => owner.GetSpeechAmplitudeThresholdAsync(caller.Token), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingSettings_UsesModelEvenWhenCallerAlreadyCancelled(bool callerCancelled)
    {
        using var caller = new CancellationTokenSource();
        if (callerCancelled)
        {
            caller.Cancel();
        }

        var resolver = new SpeechCaptureSpeechAmplitudeThresholdResolver();

        Assert.Equal(SpeechCaptureProfileCatalogReader.GetSegmenterProfile()!.SpeechAmplitudeThreshold,
            await resolver.GetSpeechAmplitudeThresholdAsync(caller.Token));
    }

    [Fact]
    public async Task NegativeSetting_UsesModeledThreshold()
    {
        var settings = new Mock<ISpeechCaptureAmplitudeThresholdSettings>(MockBehavior.Strict);
        settings.Setup(owner => owner.GetSpeechAmplitudeThresholdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(-1);
        var resolver = new SpeechCaptureSpeechAmplitudeThresholdResolver(settings.Object);

        Assert.Equal(SpeechCaptureProfileCatalogReader.GetSegmenterProfile()!.SpeechAmplitudeThreshold,
            await resolver.GetSpeechAmplitudeThresholdAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SettingsReadFailure_UsesModeledThreshold()
    {
        var settings = new Mock<ISpeechCaptureAmplitudeThresholdSettings>(MockBehavior.Strict);
        settings.Setup(owner => owner.GetSpeechAmplitudeThresholdAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("settings unavailable"));
        var resolver = new SpeechCaptureSpeechAmplitudeThresholdResolver(settings.Object);

        Assert.Equal(SpeechCaptureProfileCatalogReader.GetSegmenterProfile()!.SpeechAmplitudeThreshold,
            await resolver.GetSpeechAmplitudeThresholdAsync(CancellationToken.None));
    }
}
