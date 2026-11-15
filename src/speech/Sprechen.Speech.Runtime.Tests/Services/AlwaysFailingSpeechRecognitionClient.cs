using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class AlwaysFailingSpeechRecognitionClient : ISpeechRecognitionClient
{
    public int CallCount { get; private set; }

    public Task<SpeechTranscript> SpeechToTextAsync(
        CapturedSpeechAudio audio,
        string? languageCode,
        RecognitionProfile recognitionProfile,
        CancellationToken cancellationToken,
        SpeechKeyterms? keyterms = null)
    {
        CallCount++;
        throw new InvalidOperationException("synthetic failure");
    }

    public Task<DetectedLanguage> DetectLanguageAsync(CapturedSpeechAudio audio, CancellationToken cancellationToken) =>
        Task.FromResult(new DetectedLanguage("de", "German", Array.Empty<DetectedLanguageCandidate>()));
}
