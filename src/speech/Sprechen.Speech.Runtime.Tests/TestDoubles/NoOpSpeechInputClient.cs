
namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class NoOpSpeechInputClient : ISpeechInputClient
{
    public Task RequestMicrophoneAccessAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task EnsureCaptureSessionReadyAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<CapturedSpeechAudio> RecordAsync(CancellationToken cancellationToken)
        => Task.FromException<CapturedSpeechAudio>(new NoSpeechDetectedException());


    public Task<SpeechTranscript> SpeechToTextAsync(CapturedSpeechAudio utterance, string? languageCode, RecognitionProfile recognitionProfile, CancellationToken cancellationToken, SpeechKeyterms? keyterms = null)
        => Task.FromException<SpeechTranscript>(new InvalidOperationException("Speech utterance client is not configured for this test."));
}


