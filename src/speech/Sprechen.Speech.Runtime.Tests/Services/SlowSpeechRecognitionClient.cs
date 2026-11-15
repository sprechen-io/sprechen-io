using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class SlowSpeechRecognitionClient : ISpeechRecognitionClient
{
    private readonly TaskCompletionSource _firstCallObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool LastCallObservedCancellation { get; private set; }

    public async Task<SpeechTranscript> SpeechToTextAsync(
        CapturedSpeechAudio audio,
        string? languageCode,
        RecognitionProfile recognitionProfile,
        CancellationToken cancellationToken,
        SpeechKeyterms? keyterms = null)
    {
        _firstCallObserved.TrySetResult();
        try
        {
            await using var registration = cancellationToken.Register(() => _release.TrySetResult());
            await _release.Task.ConfigureAwait(false);
        }
        catch
        {
        }

        if (cancellationToken.IsCancellationRequested)
        {
            LastCallObservedCancellation = true;
            cancellationToken.ThrowIfCancellationRequested();
        }

        return new SpeechTranscript("ok", 100);
    }

    public Task WaitForFirstCallAsync(TimeSpan timeout) =>
        _firstCallObserved.Task.WaitAsync(timeout);

    public void ReleaseAll() => _release.TrySetResult();
}
