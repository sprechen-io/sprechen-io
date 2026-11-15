using Microsoft.Extensions.Logging;
using Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft begrenzte erneute Aufnahme nach leeren Erkennungsergebnissen innerhalb
/// einer laufenden Aufnahmeanzeige. Vor erneuter Aufnahme muss der Verarbeitungston
/// enden. Nach drei leeren Ergebnissen wird RecognizedAsEmpty zurückgegeben;
/// technische Fehler und Stille vor Sprachbeginn behalten ihre eigene Einordnung.
/// </summary>
public sealed class SelectionDialogSilentRelistenTests
{
    private static CapturedSpeechAudio Utterance() =>
        new(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);

    private static Mock<ISpeechCaptureRecordingIndicator> CreateRecordingIndicatorSpy()
    {
        var indicator = new Mock<ISpeechCaptureRecordingIndicator>();
        indicator
            .Setup(i => i.BeginRecordingAsync())
            .Returns(new ValueTask<IAsyncDisposable>(Mock.Of<IAsyncDisposable>()));
        indicator
            .Setup(i => i.EnsureStoppedAsync())
            .Returns(ValueTask.CompletedTask);
        indicator
            .Setup(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        return indicator;
    }

    private static SelectionDialogListeningTurn CreateListeningTurn(
        ISpeechCaptureRecordingIndicator indicator,
        ScriptedSpeechInputClient speechClient)
        => CreateListeningTurn(indicator, speechClient, NullLogger.Instance);

    private static SelectionDialogListeningTurn CreateListeningTurn(
        ISpeechCaptureRecordingIndicator indicator,
        ScriptedSpeechInputClient speechClient,
        ILogger logger)
    {
        var transcriber = new SelectionDialogSpeechTranscriber(new SpeechRecognitionWorker(speechClient));
        var failureMapper = new SelectionDialogRecoverableSpeechFailureMapper(NullLogger.Instance);
        return new SelectionDialogListeningTurn(
            indicator,
            speechClient,
            transcriber,
            failureMapper,
            logger);
    }

    [Fact]
    public async Task ListenAsync_CapturedButEmptyThenSpeech_SilentlyRelistensWithoutNewListeningCue()
    {
        // Ein leeres Ergebnis gefolgt von brauchbarem Text soll dieselbe Aufnahmeanzeige
        // behalten, damit kein neuer Bereitschaftshinweis angefordert wird.
        var indicator = CreateRecordingIndicatorSpy();
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(Utterance()));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(Utterance()));
        speechClient.EnqueueTranscript("");
        speechClient.EnqueueTranscript("Einfaches Beispiel");
        var turn = CreateListeningTurn(indicator.Object, speechClient);

        var result = await turn.ListenAsync("de", noInputTimeoutMs: 500, CancellationToken.None);

        Assert.Equal("Einfaches Beispiel", result.Transcript);
        Assert.False(result.RecognizedAsEmpty);
        Assert.Equal(2, speechClient.RecordCallCount);
        indicator.Verify(i => i.BeginRecordingAsync(), Times.Once);
        // Vor erneuter Aufnahme den Verarbeitungston beenden; es gibt hier keinen neuen BeginRecordingAsync-Aufruf für diese Bereinigung.
        indicator.Verify(i => i.StopProcessingCueAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListenAsync_CapturedButEmptyRepeatedly_IsBoundedAndFallsThroughToNoInput()
    {
        // Drei leere Ergebnisse erreichen die Grenze. Nur vor den ersten beiden
        // Wiederholungen wird der Verarbeitungston gestoppt; danach entscheidet der Aufrufer über die Rückmeldung.
        var indicator = CreateRecordingIndicatorSpy();
        var speechClient = new ScriptedSpeechInputClient();
        for (var i = 0; i < 3; i++)
        {
            await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(Utterance()));
            speechClient.EnqueueTranscript("");
        }

        var turn = CreateListeningTurn(indicator.Object, speechClient);

        var result = await turn.ListenAsync("de", noInputTimeoutMs: 500, CancellationToken.None);

        Assert.Null(result.Transcript);
        Assert.True(result.RecognizedAsEmpty);
        Assert.Equal(3, speechClient.RecordCallCount);
        indicator.Verify(i => i.BeginRecordingAsync(), Times.Once);
        indicator.Verify(i => i.StopProcessingCueAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ListenAsync_WhenTheReArmLoopIsExhausted_SaysSoAtWarning()
    {
        // Die ausgeschöpfte Grenze muss als Warnung mit der Zahl der Versuche sichtbar sein.
        var indicator = CreateRecordingIndicatorSpy();
        var speechClient = new ScriptedSpeechInputClient();
        for (var i = 0; i < 3; i++)
        {
            await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(Utterance()));
            speechClient.EnqueueTranscript("");
        }

        var logger = new RecordingLogger<SelectionDialogSilentRelistenTests>();
        var turn = CreateListeningTurn(indicator.Object, speechClient, logger);

        await turn.ListenAsync("de", noInputTimeoutMs: 500, CancellationToken.None);

        var exhausted = Assert.Single(
            logger.Entries,
            entry => entry.EventId == SelectionRuntimeLogEvents.CapturedButEmptyReArmExhausted);
        Assert.Equal(LogLevel.Warning, exhausted.Level);
        Assert.Contains("3", exhausted.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListenAsync_WhenTheTurnRecovers_DoesNotClaimExhaustion()
    {
        // Ein leerer Versuch mit anschließendem brauchbarem Text ist erfolgreiche Wiederherstellung, keine Warnung über ausgeschöpfte Versuche.
        var indicator = CreateRecordingIndicatorSpy();
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(Utterance()));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(Utterance()));
        speechClient.EnqueueTranscript("");
        speechClient.EnqueueTranscript("Einfaches Beispiel");

        var logger = new RecordingLogger<SelectionDialogSilentRelistenTests>();
        var turn = CreateListeningTurn(indicator.Object, speechClient, logger);

        var result = await turn.ListenAsync("de", noInputTimeoutMs: 500, CancellationToken.None);

        Assert.Equal("Einfaches Beispiel", result.Transcript);
        Assert.DoesNotContain(
            logger.Entries,
            entry => entry.EventId == SelectionRuntimeLogEvents.CapturedButEmptyReArmExhausted);
    }

    [Fact]
    public async Task ListenAsync_RecognitionThatDidNotComplete_IsNotTreatedAsCapturedButEmpty()
    {
        // Ein fehlgeschlagener Erkennungsaufruf ohne Aufruferabbruch ist kein leeres
        // Erkennungsergebnis. Er muss nach einer Aufnahme als behebbarer Fehler zurückkehren.
        var indicator = CreateRecordingIndicatorSpy();
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(Utterance()));
        speechClient.EnqueueSpeechToTextFault(new OperationCanceledException("recognition did not complete"));
        var turn = CreateListeningTurn(indicator.Object, speechClient);

        var result = await turn.ListenAsync("de", noInputTimeoutMs: 500, CancellationToken.None);

        Assert.False(result.RecognizedAsEmpty);
        Assert.Null(result.Transcript);
        Assert.Equal(SelectionDialogRecoverableSpeechFailureMapper.CaptureInterruptedFailureCode, result.FailureCode);
        Assert.True(result.Recoverable);

        Assert.Equal(1, speechClient.RecordCallCount);
    }

    [Fact]
    public async Task ListenAsync_GenuineSilence_StillReturnsPlainNoTranscript()
    {
        // Stille vor einem erfassten Segment liefert keinen Text und kein RecognizedAsEmpty; sie startet keine Schleife für leere Erkennungsergebnisse.
        var indicator = CreateRecordingIndicatorSpy();
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => throw new NoSpeechDetectedException());
        var turn = CreateListeningTurn(indicator.Object, speechClient);

        var result = await turn.ListenAsync("de", noInputTimeoutMs: 500, CancellationToken.None);

        Assert.Null(result.Transcript);
        Assert.False(result.RecognizedAsEmpty);
        Assert.Equal(1, speechClient.RecordCallCount);
    }

    [Fact]
    public async Task Transcriber_MarksCapturedButEmpty_AndLeavesSpeechUnmarked()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var transcriber = new SelectionDialogSpeechTranscriber(new SpeechRecognitionWorker(speechClient));

        speechClient.EnqueueTranscript("   ");
        var empty = await transcriber.TranscribeAsync(Utterance(), "de", CancellationToken.None);
        Assert.Null(empty.Transcript);
        Assert.True(empty.RecognizedAsEmpty);

        speechClient.EnqueueTranscript("ja");
        var speech = await transcriber.TranscribeAsync(Utterance(), "de", CancellationToken.None);
        Assert.Equal("ja", speech.Transcript);
        Assert.False(speech.RecognizedAsEmpty);
    }
}
