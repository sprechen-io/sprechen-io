using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.ModelRuntime.Services;
using Sprechen.Speech.TestDoubles;
using Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, dass ein leeres oder nur aus Leerzeichen bestehendes Erkennungsergebnis
/// als RecognizedAsEmpty markiert wird, ein Transkript mit Wörtern dagegen nicht.
/// Diese Kennzeichnung unterscheidet ein leeres Erkennungsergebnis von einem
/// vorgelagerten Aufnahme-Timeout. Die Testdaten belegen keine tatsächlich gesprochene Äußerung.
/// </summary>
public sealed class FormRecognitionEmptyResultMarkingTests
{
    [Fact]
    public async Task AnEmptyRecognition_IsMarkedAsRecognizedEmpty()
    {
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(Utterance());
        speechClient.EnqueueTranscript(string.Empty);

        var result = await RunAsync(speechClient);

        Assert.Null(result.Transcript);
        Assert.True(
            result.RecognizedAsEmpty,
            "an empty recognition must say so, or the dialog records it as a participant who was silent");
    }

    [Fact]
    public async Task AWhitespaceOnlyRecognition_IsMarkedTheSameWay()
    {
        // Leerzeichen werden bei der Auswertung entfernt und ergeben dieselbe Markierung wie ein leerer Text.
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(Utterance());
        speechClient.EnqueueTranscript("   ");

        var result = await RunAsync(speechClient);

        Assert.True(result.RecognizedAsEmpty);
    }

    [Fact]
    public async Task ARecognitionWithWords_IsNotMarked()
    {
        // Der Gegenfall verhindert, dass jedes Ergebnis pauschal als leer markiert wird.
        var speechClient = new QueuedSpeechInputClient();
        await speechClient.EnqueueRecordingAsync(Utterance());
        speechClient.EnqueueTranscript("Max");

        var result = await RunAsync(speechClient);

        Assert.Equal("Max", result.Transcript);
        Assert.False(result.RecognizedAsEmpty);
    }

    private static CapturedSpeechAudio Utterance()
        => new(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2 }]);

    private static async Task<SpeechCaptureResult> RunAsync(QueuedSpeechInputClient speechClient)
    {
        var runner = new FormVoiceCommandSpeechRecognitionRunner(
            new SpeechRecognitionWorker(speechClient),
            NullLogger.Instance);

        return await runner.RecognizeAsync(
            Utterance(),
            "de",
            RecognitionProfile.Conversational,
            keyterms: null,
            CancellationToken.None);
    }
}
