using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.App.ClientInfrastructure.Services;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechCaptureSilenceDebounceResolverTests
{
    [Fact]
    public async Task GetSilenceDebounceMsAsync_WhenSettingExceedsTheModeledFloor_KeepsTheUserSetting()
    {
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(2750));
        var resolver = new SpeechCaptureSilenceDebounceResolver(
            settings.Object,
            NullLogger<SpeechCaptureSilenceDebounceResolver>.Instance);

        var result = await resolver.GetSilenceDebounceMsAsync(SpeechCaptureMode.Listening, CancellationToken.None);

        Assert.Equal(2750, result);
    }

    [Theory]
    [InlineData(SpeechCaptureMode.Listening)]
    [InlineData(SpeechCaptureMode.ListeningContinuation)]
    [InlineData(SpeechCaptureMode.ActivationListening)]
    [InlineData(SpeechCaptureMode.ActivationListeningContinuation)]
    [InlineData(SpeechCaptureMode.ShortConfirmation)]
    [InlineData(SpeechCaptureMode.PromptInterruption)]
    [InlineData(SpeechCaptureMode.PromptInterruptionDetection)]
    public async Task EveryMode_StillResolvesAUsableDebounce_WhenTheBrowserReportsNotSet(SpeechCaptureMode mode)
    {
        // Auch ein gelesener Nullwert muss für jeden aufgeführten Modus zu einer positiven Stillegrenze führen.
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(0));
        var resolver = new SpeechCaptureSilenceDebounceResolver(
            settings.Object,
            NullLogger<SpeechCaptureSilenceDebounceResolver>.Instance);

        var result = await resolver.GetSilenceDebounceMsAsync(mode, CancellationToken.None);

        Assert.True(result > 0, $"Mode {mode} resolved {result} ms, which the segmenter rejects.");
    }

    [Fact]
    public async Task GetSilenceDebounceMsAsync_ActivationListeningWhenSettingBelowModeledFloor_RaisesToFloor()
    {
        var modeledFloor = SpeechCaptureProfileCatalogReader.GetSilenceDebounceMs(SpeechCaptureMode.ActivationListening) ?? 0;
        Assert.True(modeledFloor > 500, "The modeled activation floor must exceed the listening minimum.");

        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(500));
        var resolver = new SpeechCaptureSilenceDebounceResolver(
            settings.Object,
            NullLogger<SpeechCaptureSilenceDebounceResolver>.Instance);

        var result = await resolver.GetSilenceDebounceMsAsync(SpeechCaptureMode.ActivationListening, CancellationToken.None);

        Assert.Equal(modeledFloor, result);
        Assert.Equal(Math.Max(500, modeledFloor), result);
    }

    [Fact]
    public async Task GetSilenceDebounceMsAsync_ActivationListeningWhenSettingAboveModeledFloor_KeepsLongerSetting()
    {
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(3000));
        var resolver = new SpeechCaptureSilenceDebounceResolver(
            settings.Object,
            NullLogger<SpeechCaptureSilenceDebounceResolver>.Instance);

        var result = await resolver.GetSilenceDebounceMsAsync(SpeechCaptureMode.ActivationListening, CancellationToken.None);

        Assert.Equal(3000, result);
    }

    [Fact]
    public async Task GetSilenceDebounceMsAsync_UnmodeledMode_StillClearsTheModeledListeningFloor()
    {
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(275));
        var resolver = new SpeechCaptureSilenceDebounceResolver(
            settings.Object,
            NullLogger<SpeechCaptureSilenceDebounceResolver>.Instance);

        var result = await resolver.GetSilenceDebounceMsAsync((SpeechCaptureMode)999, CancellationToken.None);

        // Auch ein unbekannter Modus darf die modellierte Listening-Mindestgrenze nicht unterschreiten.
        Assert.Equal(SpeechCaptureProfileCatalogReader.GetSilenceDebounceMs(SpeechCaptureMode.Listening) ?? 0, result);
    }

    [Fact]
    public async Task GetSilenceDebounceMsAsync_WhenSettingsFail_FallsBackToDefaultValue()
    {
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var resolver = new SpeechCaptureSilenceDebounceResolver(
            settings.Object,
            NullLogger<SpeechCaptureSilenceDebounceResolver>.Instance);

        var result = await resolver.GetSilenceDebounceMsAsync(SpeechCaptureMode.Listening, CancellationToken.None);

        Assert.Equal(SpeechCaptureProfileCatalogReader.GetSilenceDebounceMs(SpeechCaptureMode.Listening) ?? 0, result);
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
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings.Setup(owner => owner.GetSilenceDebounceMsAsync(caller.Token)).ThrowsAsync(failure);
        var resolver = new SpeechCaptureSilenceDebounceResolver(settings.Object, NullLogger<SpeechCaptureSilenceDebounceResolver>.Instance);

        if (callerCancelled)
        {
            var actual = await Assert.ThrowsAsync<OperationCanceledException>(() => resolver.GetSilenceDebounceMsAsync(SpeechCaptureMode.Listening, caller.Token));
            Assert.Same(failure, actual);
        }
        else
        {
            Assert.Equal(SpeechCaptureProfileCatalogReader.GetSilenceDebounceMs(SpeechCaptureMode.Listening) ?? 0, await resolver.GetSilenceDebounceMsAsync(SpeechCaptureMode.Listening, caller.Token));
        }

        settings.Verify(owner => owner.GetSilenceDebounceMsAsync(caller.Token), Times.Once);
    }
}
