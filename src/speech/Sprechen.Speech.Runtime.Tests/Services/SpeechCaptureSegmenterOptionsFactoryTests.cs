using Microsoft.Extensions.Options;
using Sprechen.Mda.Contracts.V1.SpeechCapture;
using Sprechen.Speech.App.ClientInfrastructure.Services;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechCaptureSegmenterOptionsFactoryTests
{
    [Fact]
    public void CreateOptions_WhenListeningWithoutOverride_UsesModeledSegmenterNotInjectedDefaults()
    {
        var baseOptions = CreateBaseOptions();
        var factory = CreateFactory(new SpeechCaptureClientOptions(), baseOptions);
        var modeled = SpeechCaptureProfileCatalogReader.GetSegmenterProfile();
        Assert.NotNull(modeled);
        Assert.NotEqual(modeled.SpeechAmplitudeThreshold, baseOptions.SpeechAmplitudeThreshold);

        var result = factory.CreateOptions(SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening), initialSilenceTimeout: null);

        Assert.NotSame(baseOptions, result);
        AssertModeledSegmenter(modeled, result);
        Assert.Equal(baseOptions.MaxUtteranceDuration, result.MaxUtteranceDuration);
        Assert.Equal(baseOptions.MaxInitialSilence, result.MaxInitialSilence);
        Assert.Equal(baseOptions.PcmSampleRateHz, result.PcmSampleRateHz);
    }

    [Fact]
    public void CreateOptions_WhenActivationListeningMode_UsesWakeWordProfileOverrides()
    {
        var baseOptions = CreateBaseOptions();
        var factory = CreateFactory(new SpeechCaptureClientOptions(), baseOptions);
        var mode = SpeechCaptureMode.ActivationListening;

        var result = factory.CreateOptions(
            SpeechCaptureModeBehavior.For(mode),
            initialSilenceTimeout: null);

        Assert.NotSame(baseOptions, result);
        Assert.Equal(baseOptions.MaxInitialSilence, result.MaxInitialSilence);
        Assert.Equal(
            SpeechCaptureProfileCatalogReader.GetSpeechStartConfirmation(mode)
                ?? baseOptions.SpeechStartConfirmation,
            result.SpeechStartConfirmation);
        Assert.Equal(
            SpeechCaptureProfileCatalogReader.GetMaxUtteranceDuration(mode)
                ?? baseOptions.MaxUtteranceDuration,
            result.MaxUtteranceDuration);
        AssertModeledSegmenter(SpeechCaptureProfileCatalogReader.GetSegmenterProfile()!, result);
        Assert.Equal(baseOptions.PcmSampleRateHz, result.PcmSampleRateHz);
    }

    [Fact]
    public void CreateOptions_WhenActivationListeningContinuationMode_UsesContinuationProfileOverrides()
    {
        var baseOptions = CreateBaseOptions();
        var factory = CreateFactory(new SpeechCaptureClientOptions(), baseOptions);
        var mode = SpeechCaptureMode.ActivationListeningContinuation;

        var result = factory.CreateOptions(
            SpeechCaptureModeBehavior.For(mode),
            initialSilenceTimeout: null);

        Assert.NotSame(baseOptions, result);
        Assert.Equal(baseOptions.MaxInitialSilence, result.MaxInitialSilence);
        Assert.Equal(
            SpeechCaptureProfileCatalogReader.GetSpeechStartConfirmation(mode)
                ?? baseOptions.SpeechStartConfirmation,
            result.SpeechStartConfirmation);
        Assert.Equal(
            SpeechCaptureProfileCatalogReader.GetMaxUtteranceDuration(mode)
                ?? baseOptions.MaxUtteranceDuration,
            result.MaxUtteranceDuration);
        AssertModeledSegmenter(SpeechCaptureProfileCatalogReader.GetSegmenterProfile()!, result);
        Assert.Equal(baseOptions.PcmSampleRateHz, result.PcmSampleRateHz);
    }

    [Fact]
    public void CreateOptions_WhenShortConfirmationMode_RelaxesRecentPlaybackGuardButKeepsStrictOnset()
    {
        var baseOptions = CreateBaseOptions();
        var captureOptions = new SpeechCaptureClientOptions
        {
            ShortConfirmationRecentPlaybackSpeechStartGuard = TimeSpan.FromMilliseconds(60)
        };
        var factory = CreateFactory(captureOptions, baseOptions);

        var result = factory.CreateOptions(
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.ShortConfirmation),
            initialSilenceTimeout: null);

        Assert.NotSame(baseOptions, result);
        Assert.Equal(TimeSpan.FromMilliseconds(60), result.RecentPlaybackSpeechStartGuard);
        Assert.Equal(baseOptions.SpeechStartConfirmation, result.SpeechStartConfirmation);
        Assert.Equal(baseOptions.MaxUtteranceDuration, result.MaxUtteranceDuration);
        Assert.Equal(baseOptions.MaxInitialSilence, result.MaxInitialSilence);
        AssertModeledSegmenter(SpeechCaptureProfileCatalogReader.GetSegmenterProfile()!, result);
    }

    [Fact]
    public void CreateOptions_WhenShortConfirmationMode_DoesNotMutateListeningFieldCaptureOptions()
    {
        var baseOptions = CreateBaseOptions();
        var factory = CreateFactory(
            new SpeechCaptureClientOptions { ShortConfirmationRecentPlaybackSpeechStartGuard = TimeSpan.FromMilliseconds(60) },
            baseOptions);

        _ = factory.CreateOptions(SpeechCaptureModeBehavior.For(SpeechCaptureMode.ShortConfirmation), initialSilenceTimeout: null);
        var listening = factory.CreateOptions(SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening), initialSilenceTimeout: null);

        Assert.Equal(TimeSpan.FromMilliseconds(140), listening.RecentPlaybackSpeechStartGuard);
        Assert.Equal(baseOptions.MaxUtteranceDuration, listening.MaxUtteranceDuration);
        AssertModeledSegmenter(SpeechCaptureProfileCatalogReader.GetSegmenterProfile()!, listening);
    }

    [Fact]
    public void CreateOptions_WhenInitialSilenceTimeoutOverrideIsProvided_OverridesOnlyTimeout()
    {
        var baseOptions = CreateBaseOptions();
        var factory = CreateFactory(new SpeechCaptureClientOptions(), baseOptions);

        var result = factory.CreateOptions(
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening),
            TimeSpan.FromMilliseconds(900));

        Assert.NotSame(baseOptions, result);
        Assert.Equal(TimeSpan.FromMilliseconds(900), result.MaxInitialSilence);
        Assert.Equal(baseOptions.SpeechStartConfirmation, result.SpeechStartConfirmation);
        Assert.Equal(baseOptions.MaxUtteranceDuration, result.MaxUtteranceDuration);
        AssertModeledSegmenter(SpeechCaptureProfileCatalogReader.GetSegmenterProfile()!, result);
        Assert.Equal(baseOptions.PcmSampleRateHz, result.PcmSampleRateHz);
    }

    private static SpeechCaptureSegmenterOptionsFactory CreateFactory(
        SpeechCaptureClientOptions captureOptions,
        PcmSpeechCaptureSegmenterOptions segmenterOptions)
    {
        return new SpeechCaptureSegmenterOptionsFactory(
            Options.Create(captureOptions),
            Options.Create(segmenterOptions));
    }

    private static PcmSpeechCaptureSegmenterOptions CreateBaseOptions()
    {
        return new PcmSpeechCaptureSegmenterOptions
        {
            Pcm16BytesPerSample = 2,
            PcmSampleRateHz = 22050,
            SpeechAmplitudeThreshold = 345,
            SpeechStartAdaptiveMargin = 111,
            ActivePlaybackSpeechStartAdaptiveMargin = 222,
            IdleAdaptiveFloorMaxMargin = 133,
            PlaybackLeakageBypassAmplitudeThreshold = 999,
            ActivePlaybackCalibrationChunkCount = 8,
            RecentPlaybackCalibrationChunkCount = 3,
            MaxPreRollChunks = 7,
            InitialChunkCapacity = 300,
            MaxInitialSilence = TimeSpan.FromSeconds(12),
            RecentPlaybackSpeechStartGuard = TimeSpan.FromMilliseconds(140),
            SpeechStartConfirmation = TimeSpan.FromMilliseconds(120),
            MaxUtteranceDuration = TimeSpan.FromSeconds(6),
            TailSilence = TimeSpan.FromMilliseconds(240)
        };
    }

    private static void AssertModeledSegmenter(
        SpeechCaptureSegmenterProfile modeled,
        PcmSpeechCaptureSegmenterOptions actual)
    {
        Assert.Equal(modeled.SpeechAmplitudeThreshold, actual.SpeechAmplitudeThreshold);
        Assert.Equal(modeled.SpeechStartAdaptiveMargin, actual.SpeechStartAdaptiveMargin);
        Assert.Equal(modeled.ActivePlaybackSpeechStartAdaptiveMargin, actual.ActivePlaybackSpeechStartAdaptiveMargin);
        Assert.Equal(modeled.IdleAdaptiveFloorMaxMargin, actual.IdleAdaptiveFloorMaxMargin);
        Assert.Equal(modeled.PlaybackLeakageBypassAmplitudeThreshold, actual.PlaybackLeakageBypassAmplitudeThreshold);
        Assert.Equal(modeled.ActivePlaybackCalibrationChunkCount, actual.ActivePlaybackCalibrationChunkCount);
        Assert.Equal(modeled.RecentPlaybackCalibrationChunkCount, actual.RecentPlaybackCalibrationChunkCount);
        Assert.Equal(modeled.MaxPreRollChunks, actual.MaxPreRollChunks);
        Assert.Equal(TimeSpan.FromMilliseconds(modeled.TailSilenceMs), actual.TailSilence);
    }
}
