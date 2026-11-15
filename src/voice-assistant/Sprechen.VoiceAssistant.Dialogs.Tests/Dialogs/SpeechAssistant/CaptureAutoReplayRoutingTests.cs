using Moq;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die automatische Wiederholung bei simuliertem Ausbleiben einer Spracheingabe.
/// Ein nicht leeres Transkript wird dagegen weitergereicht und löst keine Wiederholung aus.
/// Die Tests simulieren Erfassungsergebnisse; sie messen weder Sprechbeginn noch Wartezeit.
/// </summary>
public sealed class CaptureAutoReplayRoutingTests
{
    private const string Announcement = "Das Datum muss in der Vergangenheit liegen.";

    [Fact]
    public async Task CaptureAsync_Silence_AutoReplaysTheCurrentFieldAnnouncementOnceAtTheTerminal()
    {
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

        var (capture, recordingSpeech, buffer, field, turnContext) = CaptureTurnFixture.Create(captureSpeech.Object);
        buffer.BeginFieldTurn(field.FieldId);
        buffer.Record(Announcement);

        var result = await capture.CaptureAsync(turnContext, InputMode.Voice, CancellationToken.None);

        Assert.True(result.HasFinalResult); // Abschluss ohne erfassten Wert.
        var replay = Assert.Single(recordingSpeech.ReplayCalls);
        Assert.Equal($"Noch einmal. {Announcement}", replay.Text);
    }

    [Fact]
    public async Task CaptureAsync_NonEmptyTranscript_IsCapturedAndNeverAutoReplays()
    {
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
            .ReturnsAsync(new SpeechCaptureResult("dreizehnter mai", null));

        var (capture, recordingSpeech, buffer, field, turnContext) = CaptureTurnFixture.Create(captureSpeech.Object);
        // Auch bei vorhandenem Hinweis wird ein erfasster Text weitergereicht.
        buffer.BeginFieldTurn(field.FieldId);
        buffer.Record(Announcement);

        var result = await capture.CaptureAsync(turnContext, InputMode.Voice, CancellationToken.None);

        Assert.False(result.HasFinalResult); // Erfasster Wert ohne abschließendes Ergebnis.
        Assert.Equal("dreizehnter mai", result.RequiredTranscript);
        Assert.Empty(recordingSpeech.ReplayCalls);
        // Der Hinweis bleibt für eine spätere Wiederholung gespeichert.
        Assert.Equal(Announcement, buffer.CurrentFieldTurnAnnouncement);
    }
}
