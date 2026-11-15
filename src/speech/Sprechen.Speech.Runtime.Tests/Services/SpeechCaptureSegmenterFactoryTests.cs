using Microsoft.Extensions.Options;
using Sprechen.Speech.App.ClientInfrastructure.Services;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechCaptureSegmenterFactoryTests
{
    [Fact]
    public void CreateSegmenter_StandardMode_ReturnsSegmenterWithBaseOptions()
    {
        var factory = CreateFactory();
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);

        var segmenter = factory.CreateSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            behavior,
            initialSilenceTimeout: null);

        Assert.NotNull(segmenter);
    }

    [Fact]
    public void CreateSegmenter_ActivationMode_AppliesActivationProfile()
    {
        var captureOptions = new SpeechCaptureClientOptions();
        var segmenterOptions = new PcmSpeechCaptureSegmenterOptions
        {
            SpeechStartConfirmation = TimeSpan.FromMilliseconds(100),
            MaxUtteranceDuration = TimeSpan.FromSeconds(5)
        };
        var factory = CreateFactory(captureOptions: captureOptions, segmenterOptions: segmenterOptions);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.ActivationListening);

        var segmenter = factory.CreateSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            behavior,
            initialSilenceTimeout: null);

        Assert.NotNull(segmenter);
    }

    [Fact]
    public void CreateSegmenter_WithInitialSilenceTimeout_AppliesTimeout()
    {
        var factory = CreateFactory();
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);

        var segmenter = factory.CreateSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            behavior,
            initialSilenceTimeout: TimeSpan.FromSeconds(3));

        Assert.NotNull(segmenter);
    }

    [Fact]
    public void CreateSegmenter_WithNegativeInitialSilenceTimeout_ThrowsArgumentOutOfRange()
    {
        var factory = CreateFactory();
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            behavior,
            initialSilenceTimeout: TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void CreateSegmenter_WithZeroInitialSilenceTimeout_ThrowsArgumentOutOfRange()
    {
        var factory = CreateFactory();
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            behavior,
            initialSilenceTimeout: TimeSpan.Zero));
    }

    [Fact]
    public void CreateSegmenter_ActivationMode_WithZeroOverrides_FallsBackToBaseOptions()
    {
        var captureOptions = new SpeechCaptureClientOptions();
        var factory = CreateFactory(captureOptions: captureOptions);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.ActivationListening);

        var segmenter = factory.CreateSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            behavior,
            initialSilenceTimeout: null);

        Assert.NotNull(segmenter);
    }

    private static SpeechCaptureSegmenterFactory CreateFactory(
        SpeechCaptureClientOptions? captureOptions = null,
        PcmSpeechCaptureSegmenterOptions? segmenterOptions = null)
    {
        var optionsFactory = new SpeechCaptureSegmenterOptionsFactory(
            Options.Create(captureOptions ?? new SpeechCaptureClientOptions()),
            Options.Create(segmenterOptions ?? new PcmSpeechCaptureSegmenterOptions()));
        return new SpeechCaptureSegmenterFactory(
            optionsFactory);
    }
}
