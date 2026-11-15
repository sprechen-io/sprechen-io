using AppRecognitionProfile = Sprechen.Speech.App.Contracts.Services.RecognitionProfile;

namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class CapturingRecognitionClient : ISpeechRecognitionClient
{
    public AppRecognitionProfile? LastProfile { get; private set; }

    public SpeechKeyterms? LastKeyterms { get; private set; }

    public Task<SpeechTranscript> SpeechToTextAsync(
        CapturedSpeechAudio utterance,
        string? languageCode,
        AppRecognitionProfile recognitionProfile,
        CancellationToken cancellationToken,
        SpeechKeyterms? keyterms = null)
    {
        LastProfile = recognitionProfile;
        LastKeyterms = keyterms;
        return Task.FromResult(new SpeechTranscript("ok", 90));
    }
}
