using Moq;
using Sprechen.Speech.WakeWord.Contracts;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class WakeRuntimeDependencyBundlesTests
{
    [Fact]
    public void Detection_RoundTripsCollaborators()
    {
        var provider = Mock.Of<IWakeWordProvider>();
        var resolver = Mock.Of<IAcousticWakeWordDetectorConfigurationResolver>();
        var scorer = Mock.Of<IAcousticWakeWordScorer>();
        var triggerPolicy = Mock.Of<IAcousticWakeWordTriggerPolicy>();

        var detection = new WakeWordDetectionDependencies(provider, resolver, scorer, triggerPolicy);

        Assert.Same(provider, detection.WakeWordProvider);
        Assert.Same(resolver, detection.DetectorConfigurationResolver);
        Assert.Same(scorer, detection.AcousticWakeWordScorer);
        Assert.Same(triggerPolicy, detection.AcousticWakeWordTriggerPolicy);
    }

    [Fact]
    public void Detection_NullCollaborators_Throw()
    {
        var provider = Mock.Of<IWakeWordProvider>();
        var resolver = Mock.Of<IAcousticWakeWordDetectorConfigurationResolver>();
        var scorer = Mock.Of<IAcousticWakeWordScorer>();
        var triggerPolicy = Mock.Of<IAcousticWakeWordTriggerPolicy>();

        Assert.Throws<ArgumentNullException>(() => new WakeWordDetectionDependencies(null!, resolver, scorer, triggerPolicy));
        Assert.Throws<ArgumentNullException>(() => new WakeWordDetectionDependencies(provider, null!, scorer, triggerPolicy));
        Assert.Throws<ArgumentNullException>(() => new WakeWordDetectionDependencies(provider, resolver, null!, triggerPolicy));
        Assert.Throws<ArgumentNullException>(() => new WakeWordDetectionDependencies(provider, resolver, scorer, null!));
    }

    [Fact]
    public void Capture_RoundTripsCollaborators_RequiresContinuousSegmentSource()
    {
        var worker = new SpeechRecognitionWorker(Mock.Of<ISpeechRecognitionClient>());
        var segmentSource = Mock.Of<IContinuousSegmentStreamSource>();

        var capture = new WakeRuntimeCaptureDependencies(worker, segmentSource);

        Assert.Same(worker, capture.SpeechRecognitionWorker);
        Assert.Same(segmentSource, capture.SegmentStreamSource);

        Assert.Throws<ArgumentNullException>(() => new WakeRuntimeCaptureDependencies(null!, segmentSource));
        Assert.Throws<ArgumentNullException>(() => new WakeRuntimeCaptureDependencies(worker, null!));
    }

    [Fact]
    public void Feedback_RoundTripsCollaborators()
    {
        var signals = Mock.Of<ISpeechInteractionSignals>();
        var telemetry = Mock.Of<IWakeWordTelemetrySink>();
        var earcon = Mock.Of<IEarconPlayer>();
        var tts = Mock.Of<ITextToSpeechPlayer>();
        var guidance = new WakeRecoveryGuidance(
            "To start, say: Okay Alex.",
            "To start, say: Okay Alex. You can also say Hey Alex or Ok Alex.",
            "en",
            missThreshold: 2,
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(300));
        var microphoneGuidance = MicrophoneGuidance();

        var feedback = new WakeRuntimeFeedbackDependencies(
            signals,
            telemetry,
            earcon,
            tts,
            guidance,
            microphoneGuidance);

        Assert.Same(signals, feedback.SpeechInteractionSignals);
        Assert.Same(telemetry, feedback.WakeWordTelemetrySink);
        Assert.Same(earcon, feedback.EarconPlayer);
        Assert.Same(tts, feedback.TextToSpeechPlayer);
        Assert.Same(guidance, feedback.RecoveryGuidance);
        Assert.Same(microphoneGuidance, feedback.MicrophoneAvailabilityGuidance);

        Assert.Throws<ArgumentNullException>(() => new WakeRuntimeFeedbackDependencies(null!, telemetry, earcon, tts, guidance, microphoneGuidance));
        Assert.Throws<ArgumentNullException>(() => new WakeRuntimeFeedbackDependencies(signals, null!, earcon, tts, guidance, microphoneGuidance));
        Assert.Throws<ArgumentNullException>(() => new WakeRuntimeFeedbackDependencies(signals, telemetry, null!, tts, guidance, microphoneGuidance));
        Assert.Throws<ArgumentNullException>(() => new WakeRuntimeFeedbackDependencies(signals, telemetry, earcon, null!, guidance, microphoneGuidance));
        Assert.Throws<ArgumentNullException>(() => new WakeRuntimeFeedbackDependencies(
            signals,
            telemetry,
            earcon,
            tts,
            recoveryGuidance: guidance,
            microphoneAvailabilityGuidance: null!));

        // Die gesprochenen Aktivierungshinweise sind optional und dürfen fehlen.
        var earconOnly = new WakeRuntimeFeedbackDependencies(
            signals,
            telemetry,
            earcon,
            tts,
            recoveryGuidance: null,
            microphoneAvailabilityGuidance: microphoneGuidance);
        Assert.Null(earconOnly.RecoveryGuidance);
    }

    [Fact]
    public void WakeRuntimeContext_FlatProperties_DelegateToBundles()
    {
        var detection = new WakeWordDetectionDependencies(
            Mock.Of<IWakeWordProvider>(),
            Mock.Of<IAcousticWakeWordDetectorConfigurationResolver>(),
            Mock.Of<IAcousticWakeWordScorer>(),
            Mock.Of<IAcousticWakeWordTriggerPolicy>());
        var capture = new WakeRuntimeCaptureDependencies(
            new SpeechRecognitionWorker(Mock.Of<ISpeechRecognitionClient>()),
            Mock.Of<IContinuousSegmentStreamSource>());
        var feedback = new WakeRuntimeFeedbackDependencies(
            Mock.Of<ISpeechInteractionSignals>(),
            Mock.Of<IWakeWordTelemetrySink>(),
            Mock.Of<IEarconPlayer>(),
            Mock.Of<ITextToSpeechPlayer>(),
            recoveryGuidance: null,
            microphoneAvailabilityGuidance: MicrophoneGuidance());

        var context = new WakeRuntimeContext(detection, capture, feedback);

        Assert.Same(detection, context.Detection);
        Assert.Same(capture, context.Capture);
        Assert.Same(feedback, context.Feedback);
        Assert.Same(detection.WakeWordProvider, context.WakeWordProvider);
        Assert.Same(detection.DetectorConfigurationResolver, context.DetectorConfigurationResolver);
        Assert.Same(detection.AcousticWakeWordScorer, context.AcousticWakeWordScorer);
        Assert.Same(detection.AcousticWakeWordTriggerPolicy, context.AcousticWakeWordTriggerPolicy);
        Assert.Same(capture.SpeechRecognitionWorker, context.SpeechRecognitionWorker);
        Assert.Same(capture.SegmentStreamSource, context.SegmentStreamSource);
        Assert.Same(feedback.SpeechInteractionSignals, context.SpeechInteractionSignals);
        Assert.Same(feedback.WakeWordTelemetrySink, context.WakeWordTelemetrySink);
        Assert.Same(feedback.EarconPlayer, context.EarconPlayer);
        Assert.Same(feedback.TextToSpeechPlayer, context.TextToSpeechPlayer);
        Assert.Same(feedback.RecoveryGuidance, context.RecoveryGuidance);
        Assert.Same(feedback.MicrophoneAvailabilityGuidance, context.MicrophoneAvailabilityGuidance);
    }

    private static MicrophoneAvailabilityGuidance MicrophoneGuidance()
        => new(
            "No microphone.",
            "Microphone access is blocked.",
            "The microphone is unavailable.",
            "The microphone is available again.",
            "en",
            unavailableThreshold: 2,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(120));
}
