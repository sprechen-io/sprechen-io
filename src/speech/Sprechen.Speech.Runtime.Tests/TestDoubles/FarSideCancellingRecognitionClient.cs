using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

/// <summary>
/// Simuliert einen Abbruch beim Erkenner, ohne den Aufrufertoken abzubrechen.
/// </summary>
internal sealed class FarSideCancellingRecognitionClient : ISpeechRecognitionClient
{
    private int _cancel = 1;

    public void StopCancelling() => Interlocked.Exchange(ref _cancel, 0);

    public Task<SpeechTranscript> SpeechToTextAsync(
        CapturedSpeechAudio utterance,
        string? languageCode,
        RecognitionProfile recognitionProfile,
        CancellationToken cancellationToken,
        SpeechKeyterms? keyterms = null)
        => Volatile.Read(ref _cancel) == 1
            ? Task.FromException<SpeechTranscript>(new OperationCanceledException("backend cancelled"))
            : Task.FromResult(new SpeechTranscript("nach dem Abbruch", null));
}
