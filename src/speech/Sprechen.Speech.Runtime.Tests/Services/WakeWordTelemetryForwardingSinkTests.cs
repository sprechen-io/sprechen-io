using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Speech.App.ClientInfrastructure.Services;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Prüft die Nachrichten und ausgewählte Level und Scopes der Wake-Word-Telemetrie
/// für die aufgeführten Ereignisse sowie die synchronen Abschlüsse des NoOp-Sinks.
/// </summary>
public sealed class WakeWordTelemetryForwardingSinkTests
{
    private const string CorrelationId = "abc123";

    [Fact]
    public async Task Listener_Started_WritesExpectedTrace()
    {
        var (sink, trace, logger) = CreateSink();

        await sink.RecordListenerSetupAsync(
            new WakeWordListenerSetupEvent(CorrelationId, WakeWordListenerSetupPhase.Started, "de", null, null, null),
            CancellationToken.None);

        var entry = Assert.Single(trace.Entries);
        Assert.Equal(SpeechAssistantTraceScope.WakeWordEffect, entry.Scope);
        Assert.Equal(SpeechAssistantTraceLevel.Information, entry.Level);
        Assert.Equal("Wake-word listener started for languageCode=de.", entry.Message);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task Listener_InventoryLoaded_WritesExpectedTrace()
    {
        var (sink, trace, _) = CreateSink();

        await sink.RecordListenerSetupAsync(
            new WakeWordListenerSetupEvent(CorrelationId, WakeWordListenerSetupPhase.InventoryLoaded, null, 3, 1, null),
            CancellationToken.None);

        var entry = Assert.Single(trace.Entries);
        Assert.Equal(SpeechAssistantTraceLevel.Information, entry.Level);
        Assert.Equal("Wake-word inventory loaded. activationFormCount=3 recognitionOnlyActivationFormCount=1", entry.Message);
    }

    [Fact]
    public async Task Listener_Failed_LogsAndTraces()
    {
        var (sink, trace, logger) = CreateSink();
        var ex = new InvalidOperationException("boom");

        await sink.RecordListenerSetupAsync(
            new WakeWordListenerSetupEvent(CorrelationId, WakeWordListenerSetupPhase.Failed, null, null, null, ex),
            CancellationToken.None);

        var traceEntry = Assert.Single(trace.Entries);
        Assert.Equal(SpeechAssistantTraceLevel.Error, traceEntry.Level);
        Assert.Equal("Wake-word listener setup failed.", traceEntry.Message);

        var logEntry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, logEntry.Level);
        Assert.Equal("Wake word listener setup failed.", logEntry.Message);
        Assert.Same(ex, logEntry.Exception);
    }

    [Fact]
    public async Task Acoustic_Detected_WritesExpectedTrace()
    {
        var (sink, trace, _) = CreateSink();

        await sink.RecordAcousticDetectionAsync(
            new WakeWordAcousticDetectionEvent(CorrelationId, WakeWordAcousticDetectionPhase.Detected, "porcupine", null),
            CancellationToken.None);

        var entry = Assert.Single(trace.Entries);
        Assert.Equal("Wake word detected via acoustic detector 'porcupine'.", entry.Message);
    }

    [Theory]
    [InlineData(WakeWordAcousticDetectionPhase.DetectorUnavailable, "Acoustic wake-word detector unavailable. Falling back to lexical activation.")]
    [InlineData(WakeWordAcousticDetectionPhase.DetectorWarmup, "Acoustic wake-word detector still warming up. Falling back to lexical activation.")]
    [InlineData(WakeWordAcousticDetectionPhase.DetectorFailure, "Acoustic wake-word detection failed. Falling back to lexical activation.")]
    public async Task Acoustic_FallbackPhases_LogOnly(WakeWordAcousticDetectionPhase phase, string expectedMessage)
    {
        var (sink, trace, logger) = CreateSink();
        var ex = new Exception("boom");

        await sink.RecordAcousticDetectionAsync(
            new WakeWordAcousticDetectionEvent(CorrelationId, phase, null, ex),
            CancellationToken.None);

        Assert.Empty(trace.Entries);
        var logEntry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, logEntry.Level);
        Assert.Equal(expectedMessage, logEntry.Message);
        Assert.Same(ex, logEntry.Exception);
    }

    [Fact]
    public async Task CaptureLoop_NoSpeech_WritesExpectedTrace()
    {
        var (sink, trace, _) = CreateSink();

        await sink.RecordCaptureLoopAsync(
            new WakeWordCaptureLoopEvent(CorrelationId, WakeWordCaptureLoopPhase.CaptureNoSpeechRetry, null),
            CancellationToken.None);

        var entry = Assert.Single(trace.Entries);
        Assert.Equal(SpeechAssistantTraceLevel.Information, entry.Level);
        Assert.Equal("Wake-word capture completed without speech and will retry.", entry.Message);
    }

    [Fact]
    public async Task CaptureLoop_CaptureFailed_LogsAndTraces()
    {
        var (sink, trace, logger) = CreateSink();
        var ex = new InvalidOperationException("capfail");

        await sink.RecordCaptureLoopAsync(
            new WakeWordCaptureLoopEvent(CorrelationId, WakeWordCaptureLoopPhase.CaptureFailedRetry, ex),
            CancellationToken.None);

        // Der Fehlergrund soll auch in der Warnung stehen, damit er nicht nur im Debuglog verfügbar ist.
        var traceEntry = Assert.Single(trace.Entries);
        Assert.Equal(SpeechAssistantTraceLevel.Warning, traceEntry.Level);
        Assert.Equal(
            "Wake-word capture failed and the listener will retry. reason=InvalidOperationException: capfail",
            traceEntry.Message);

        var logEntry = Assert.Single(logger.Entries);
        Assert.Equal("Wake word capture failed.", logEntry.Message);
        Assert.Same(ex, logEntry.Exception);
    }

    [Fact]
    public async Task CaptureLoop_CaptureFailedWithoutException_WritesBaseMessageWithoutReason()
    {
        var (sink, trace, _) = CreateSink();

        await sink.RecordCaptureLoopAsync(
            new WakeWordCaptureLoopEvent(CorrelationId, WakeWordCaptureLoopPhase.CaptureFailedRetry, Exception: null),
            CancellationToken.None);

        var traceEntry = Assert.Single(trace.Entries);
        Assert.Equal(SpeechAssistantTraceLevel.Warning, traceEntry.Level);
        Assert.Equal("Wake-word capture failed and the listener will retry.", traceEntry.Message);
    }

    [Fact]
    public async Task CaptureLoop_TranscriptionNoSpeech_WritesExpectedTrace()
    {
        var (sink, trace, _) = CreateSink();

        await sink.RecordCaptureLoopAsync(
            new WakeWordCaptureLoopEvent(CorrelationId, WakeWordCaptureLoopPhase.TranscriptionNoSpeechRetry, null),
            CancellationToken.None);

        var entry = Assert.Single(trace.Entries);
        Assert.Equal("Wake-word transcription returned no speech and the listener will retry.", entry.Message);
    }

    [Theory]
    [InlineData(WakeWordTranscriptionRejectionPhase.QueueFull, "Wake word transcription rejected: queue full.")]
    [InlineData(WakeWordTranscriptionRejectionPhase.BackoffExhausted, "Wake word transcription rejected: backoff exhausted.")]
    [InlineData(WakeWordTranscriptionRejectionPhase.MicGateHardOff, "Wake word transcription rejected: mic gate hard-off.")]
    public async Task TranscriptionRejection_Logged_LogsOnly(WakeWordTranscriptionRejectionPhase phase, string expectedMessage)
    {
        var (sink, trace, logger) = CreateSink();
        var ex = new Exception("rej");

        await sink.RecordTranscriptionRejectionAsync(
            new WakeWordTranscriptionRejectionEvent(CorrelationId, phase, null, ex),
            CancellationToken.None);

        Assert.Empty(trace.Entries);
        var logEntry = Assert.Single(logger.Entries);
        Assert.Equal(expectedMessage, logEntry.Message);
        Assert.Same(ex, logEntry.Exception);
    }

    [Fact]
    public async Task TranscriptionRejection_Dispatched_WritesExpectedTrace()
    {
        var (sink, trace, _) = CreateSink();

        await sink.RecordTranscriptionRejectionAsync(
            new WakeWordTranscriptionRejectionEvent(CorrelationId, WakeWordTranscriptionRejectionPhase.Dispatched, "QUEUE_FULL", null),
            CancellationToken.None);

        var entry = Assert.Single(trace.Entries);
        Assert.Equal(SpeechAssistantTraceLevel.Warning, entry.Level);
        Assert.Equal("Wake-word transcription rejected by worker (code=QUEUE_FULL); re-arming wake listener.", entry.Message);
    }

    [Fact]
    public async Task Stitch_Attached_WritesExpectedTrace()
    {
        var (sink, trace, _) = CreateSink();

        await sink.RecordStitchAsync(
            new WakeWordStitchEvent(CorrelationId, WakeWordStitchPhase.Attached, 0, 12, 5),
            CancellationToken.None);

        var entry = Assert.Single(trace.Entries);
        Assert.Equal(SpeechAssistantTraceScope.PostWakeWordRecovery, entry.Scope);
        Assert.Equal("wake stitched stitchedLen=12 commandTextLen=5", entry.Message);
    }

    [Fact]
    public async Task Stitch_Started_WritesExpectedTrace()
    {
        var (sink, trace, _) = CreateSink();

        await sink.RecordStitchAsync(
            new WakeWordStitchEvent(CorrelationId, WakeWordStitchPhase.Started, 7, 0, 0),
            CancellationToken.None);

        var entry = Assert.Single(trace.Entries);
        Assert.Equal("wake carried prefixLen=7", entry.Message);
    }

    [Fact]
    public async Task Stitch_Expired_WritesExpectedTrace()
    {
        var (sink, trace, _) = CreateSink();

        await sink.RecordStitchAsync(
            new WakeWordStitchEvent(CorrelationId, WakeWordStitchPhase.Expired, 5, 0, 0),
            CancellationToken.None);

        var entry = Assert.Single(trace.Entries);
        Assert.Equal("wake stitch expired previousPrefixLen=5", entry.Message);
    }

    [Fact]
    public async Task Stitch_NoOp_WritesExpectedTrace()
    {
        var (sink, trace, _) = CreateSink();

        await sink.RecordStitchAsync(
            new WakeWordStitchEvent(CorrelationId, WakeWordStitchPhase.NoOp, 4, 0, 0),
            CancellationToken.None);

        var entry = Assert.Single(trace.Entries);
        Assert.Equal("wake stitch noop transcriptLen=4", entry.Message);
    }

    [Theory]
    [InlineData(WakeWordActivationStatus.NoRecognizedSpeech, null, SpeechAssistantTraceLevel.Information, "Wake-word transcription returned no recognized speech.")]
    [InlineData(WakeWordActivationStatus.MissingLeadingWakeWord, 42, SpeechAssistantTraceLevel.Warning, "Wake-word transcription did not match the modeled inventory. confidence=42")]
    [InlineData(WakeWordActivationStatus.MissingLeadingWakeWord, null, SpeechAssistantTraceLevel.Warning, "Wake-word transcription did not match the modeled inventory. confidence=<null>")]
    public async Task Miss_Trace_WritesExpectedTrace(WakeWordActivationStatus status, int? confidence, SpeechAssistantTraceLevel expectedLevel, string expectedMessage)
    {
        var (sink, trace, _) = CreateSink();

        await sink.RecordMissAsync(
            new WakeWordMissEvent(CorrelationId, WakeWordMissPhase.MissTrace, status, confidence, null, null, null, null, null),
            CancellationToken.None);

        var entry = Assert.Single(trace.Entries);
        Assert.Equal(SpeechAssistantTraceScope.WakeWordEffect, entry.Scope);
        Assert.Equal(expectedLevel, entry.Level);
        Assert.Equal(expectedMessage, entry.Message);
    }

    [Fact]
    public async Task Miss_Diagnostic_WritesExpectedTrace()
    {
        var (sink, trace, _) = CreateSink();

        await sink.RecordMissAsync(
            new WakeWordMissEvent(
                CorrelationId,
                WakeWordMissPhase.MissDiagnostic,
                WakeWordActivationStatus.MissingLeadingWakeWord,
                33,
                8,
                4,
                2,
                null,
                new WakeWordMissDiagnosticDetail(
                    TokenCount: 1,
                    ActivationForms: new[] { "Okay" },
                    RecognitionOnlyActivationForms: new[] { "Ok" })),
            CancellationToken.None);

        var entry = Assert.Single(trace.Entries);
        Assert.Equal(SpeechAssistantTraceScope.PostWakeWordRecovery, entry.Scope);
        Assert.Equal("wake miss status=MissingLeadingWakeWord transcriptLen=8 confidence=33 activationFormCount=4 recognitionOnlyActivationFormCount=2 tokenCount=1 activationForms=[Okay] recognitionOnlyActivationForms=[Ok]", entry.Message);
    }

    [Fact]
    public async Task Miss_EarconFailed_LogsAndTraces()
    {
        var (sink, trace, logger) = CreateSink();
        var ex = new Exception("earcon");

        await sink.RecordMissAsync(
            new WakeWordMissEvent(CorrelationId, WakeWordMissPhase.EarconFailed, WakeWordActivationStatus.MissingLeadingWakeWord, null, null, null, null, ex, null),
            CancellationToken.None);

        var traceEntry = Assert.Single(trace.Entries);
        Assert.Equal(SpeechAssistantTraceLevel.Warning, traceEntry.Level);
        Assert.Equal("Wake-word miss earcon playback failed; continuing listener loop.", traceEntry.Message);

        var logEntry = Assert.Single(logger.Entries);
        Assert.Equal("Wake-word miss earcon playback failed.", logEntry.Message);
        Assert.Same(ex, logEntry.Exception);
    }

    [Fact]
    public async Task Miss_RecordingCleanupFailed_LogsAndTraces()
    {
        var (sink, trace, logger) = CreateSink();
        var ex = new Exception("cleanup");

        await sink.RecordMissAsync(
            new WakeWordMissEvent(CorrelationId, WakeWordMissPhase.RecordingCleanupFailed, WakeWordActivationStatus.LeadingWakeWordDetected, null, null, null, null, ex, null),
            CancellationToken.None);

        var traceEntry = Assert.Single(trace.Entries);
        Assert.Equal(SpeechAssistantTraceLevel.Warning, traceEntry.Level);
        Assert.Equal("Wake-word detection could not clear the recording state before transition.", traceEntry.Message);

        var logEntry = Assert.Single(logger.Entries);
        Assert.Equal("Failed to clear recording state before wake word transition.", logEntry.Message);
    }

    [Theory]
    [InlineData(true, "Wake word detected with inline command.")]
    [InlineData(false, "Wake word detected without inline command.")]
    public async Task Activation_LexicalDetected_WritesExpectedTrace(bool commandTextPresent, string expectedMessage)
    {
        var (sink, trace, _) = CreateSink();

        await sink.RecordActivationAsync(
            new WakeWordActivationEvent(CorrelationId, WakeWordActivationPhase.LexicalDetected, commandTextPresent, 0, 0, null),
            CancellationToken.None);

        var entry = Assert.Single(trace.Entries);
        Assert.Equal(SpeechAssistantTraceLevel.Information, entry.Level);
        Assert.Equal(expectedMessage, entry.Message);
    }

    [Fact]
    public async Task Activation_Diagnostic_WritesExpectedTrace()
    {
        var (sink, trace, _) = CreateSink();

        await sink.RecordActivationAsync(
            new WakeWordActivationEvent(CorrelationId, WakeWordActivationPhase.Diagnostic, true, 10, 3, 75),
            CancellationToken.None);

        var entry = Assert.Single(trace.Entries);
        Assert.Equal(SpeechAssistantTraceScope.PostWakeWordRecovery, entry.Scope);
        Assert.Equal("wake activation transcriptLen=10 commandTextLen=3 commandTextPresent=True confidence=75", entry.Message);
    }

    [Fact]
    public async Task Fallback_Published_WritesExpectedTrace()
    {
        var (sink, trace, _) = CreateSink();

        await sink.RecordFallbackAsync(
            new WakeWordFallbackEvent(CorrelationId, WakeWordFallbackPhase.Published, "detectorWarmup", null),
            CancellationToken.None);

        var entry = Assert.Single(trace.Entries);
        Assert.Equal("Wake-word listener is using lexical fallback (reasonCode=detectorWarmup).", entry.Message);
    }

    [Fact]
    public async Task Fallback_PublishFailed_LogsOnly()
    {
        var (sink, trace, logger) = CreateSink();
        var ex = new Exception("publish");

        await sink.RecordFallbackAsync(
            new WakeWordFallbackEvent(CorrelationId, WakeWordFallbackPhase.PublishFailed, null, ex),
            CancellationToken.None);

        Assert.Empty(trace.Entries);
        var logEntry = Assert.Single(logger.Entries);
        Assert.Equal("Failed to publish wake-listening fallback state.", logEntry.Message);
        Assert.Same(ex, logEntry.Exception);
    }

    [Fact]
    public void Constructor_ThrowsOnNullArgs()
    {
        var trace = new RecordingSpeechAssistantTrace();
        var logger = NullLogger<WakeWordTelemetryForwardingSink>.Instance;

        Assert.Throws<ArgumentNullException>(() => new WakeWordTelemetryForwardingSink(null!, logger));
        Assert.Throws<ArgumentNullException>(() => new WakeWordTelemetryForwardingSink(trace, null!));
    }

    [Fact]
    public void NoOpSink_AllMethodsCompleteSynchronously()
    {
        var sink = NoOpWakeWordTelemetrySink.Instance;

        Assert.True(sink.RecordListenerSetupAsync(new WakeWordListenerSetupEvent(CorrelationId, WakeWordListenerSetupPhase.Started, null, null, null, null), default).IsCompletedSuccessfully);
        Assert.True(sink.RecordAcousticDetectionAsync(new WakeWordAcousticDetectionEvent(CorrelationId, WakeWordAcousticDetectionPhase.Detected, null, null), default).IsCompletedSuccessfully);
        Assert.True(sink.RecordCaptureLoopAsync(new WakeWordCaptureLoopEvent(CorrelationId, WakeWordCaptureLoopPhase.CaptureNoSpeechRetry, null), default).IsCompletedSuccessfully);
        Assert.True(sink.RecordTranscriptionRejectionAsync(new WakeWordTranscriptionRejectionEvent(CorrelationId, WakeWordTranscriptionRejectionPhase.QueueFull, null, null), default).IsCompletedSuccessfully);
        Assert.True(sink.RecordStitchAsync(new WakeWordStitchEvent(CorrelationId, WakeWordStitchPhase.Attached, 0, 0, 0), default).IsCompletedSuccessfully);
        Assert.True(sink.RecordMissAsync(new WakeWordMissEvent(CorrelationId, WakeWordMissPhase.MissTrace, WakeWordActivationStatus.NoRecognizedSpeech, null, null, null, null, null, null), default).IsCompletedSuccessfully);
        Assert.True(sink.RecordActivationAsync(new WakeWordActivationEvent(CorrelationId, WakeWordActivationPhase.LexicalDetected, false, 0, 0, null), default).IsCompletedSuccessfully);
        Assert.True(sink.RecordFallbackAsync(new WakeWordFallbackEvent(CorrelationId, WakeWordFallbackPhase.Published, "x", null), default).IsCompletedSuccessfully);
    }

    private static (WakeWordTelemetryForwardingSink Sink, RecordingSpeechAssistantTrace Trace, RecordingLogger<WakeWordTelemetryForwardingSink> Logger) CreateSink()
    {
        var trace = new RecordingSpeechAssistantTrace();
        var logger = new RecordingLogger<WakeWordTelemetryForwardingSink>();
        var sink = new WakeWordTelemetryForwardingSink(trace, logger);
        return (sink, trace, logger);
    }
}
