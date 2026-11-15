using Moq;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.VoiceAssistant.Dialogs.Tests.Services;
using Sprechen.VoiceAssistant.Dialogs.Tests.Telemetry;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

using EvaluationTelemetryImpl = Sprechen.VoiceAssistant.EvaluationTelemetry.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Ereignisanzahl über den Aufnahme- und Fehlerbehandlungspfad eines simulierten Feldschritts.
/// Die Auswertung zählt die protokollierten Ereignisse, um doppelte Meldungen derselben Beobachtung zu erkennen.
/// </summary>
public sealed class SilentFieldTurnTelemetryChainTests
{
    [Fact]
    public async Task ASilentFieldTurn_WritesTheNoSpeechLineExactlyOnce()
    {
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var captureSpeech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        captureSpeech
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NoSpeechDetectedException());

        var session = await telemetry.StartSessionAsync(
            "test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);
        var (capture, _, _, _, turnContext) = CaptureTurnFixture.Create(captureSpeech.Object, telemetry, session);

        _ = await capture.CaptureAsync(turnContext, InputMode.Voice, CancellationToken.None);

        Assert.Equal(1, CountEvents(logger, TelemetryEventNames.NoSpeechDetected));
        Assert.Equal(0, CountEvents(logger, TelemetryEventNames.SttError));
        Assert.Equal(0, CountEvents(logger, TelemetryEventNames.ValidationErrorOpened));
    }

    [Fact]
    public async Task AnEmptyTranscriptTurn_WritesTheNoSpeechLineExactlyOnce()
    {
        // Auch ein leeres Transkript muss genau eine Meldung über fehlende Sprache erzeugen.
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var captureSpeech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        captureSpeech
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpeechCaptureResult(string.Empty, null));

        var session = await telemetry.StartSessionAsync(
            "test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);
        var (capture, _, _, _, turnContext) = CaptureTurnFixture.Create(captureSpeech.Object, telemetry, session);

        _ = await capture.CaptureAsync(turnContext, InputMode.Voice, CancellationToken.None);

        Assert.Equal(1, CountEvents(logger, TelemetryEventNames.NoSpeechDetected));
        Assert.Equal(0, CountEvents(logger, TelemetryEventNames.SttError));
    }

    [Fact]
    public async Task AnInterruptedCaptureTurn_WritesTheRecognitionLineAndNoSilenceLine()
    {
        // Eine unterbrochene Aufnahme muss als Erkennungsfehler und nicht als Stille gemeldet werden.
        var logger = new CapturingLogger<EvaluationTelemetryImpl>();
        var telemetry = CreateTelemetry(logger);
        var captureSpeech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        captureSpeech
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpeechCaptureResult(null, null, SpeechCaptureFailureCodes.CaptureInterrupted));

        var session = await telemetry.StartSessionAsync(
            "test-app", "form-1", "de", EvaluationChannel.Voice, CancellationToken.None);
        var (capture, _, _, _, turnContext) = CaptureTurnFixture.Create(captureSpeech.Object, telemetry, session);

        _ = await capture.CaptureAsync(turnContext, InputMode.Voice, CancellationToken.None);

        Assert.Equal(1, CountEvents(logger, TelemetryEventNames.SttError));
        Assert.Equal(0, CountEvents(logger, TelemetryEventNames.NoSpeechDetected));
    }

    private static int CountEvents(CapturingLogger<EvaluationTelemetryImpl> logger, string eventName)
        => logger.Entries.Count(entry =>
            entry.Values.TryGetValue("event", out var value)
            && string.Equals(value as string, eventName, StringComparison.Ordinal));

    private static IEvaluationTelemetry CreateTelemetry(CapturingLogger<EvaluationTelemetryImpl> logger)
    {
        var context = new TestStudyTelemetryContext("P123");
        var outbox = EvaluationTelemetryTestOutboxFactory.Create(context, logger);
        var sessionStateStore = new EvaluationTelemetrySessionStateStore();
        var envelopeEmitter = new EvaluationTelemetryEnvelopeEmitter(context, outbox, new Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession());
        var statusPayloads = new EvaluationStatusTelemetryPayloads();
        var pacingTelemetry = new EvaluationConversationalPacingTelemetry(
            sessionStateStore,
            envelopeEmitter,
            new EvaluationConversationalPacingTelemetryPayloads(),
            context);
        return new EvaluationTelemetryImpl(
            new EvaluationSessionTelemetry(
                context,
                sessionStateStore,
                envelopeEmitter,
                new EvaluationSessionTelemetryPayloads(),
                pacingTelemetry),
            new EvaluationFormTelemetry(
                sessionStateStore,
                envelopeEmitter,
                new EvaluationFieldTelemetryPayloads(),
                statusPayloads,
                pacingTelemetry,
                new EvaluationTelemetryDetailedStatusChangeDetector(statusPayloads)),
            new EvaluationCommandTelemetry(
                envelopeEmitter,
                new EvaluationCommandTelemetryPayloads(),
                pacingTelemetry));
    }
}
