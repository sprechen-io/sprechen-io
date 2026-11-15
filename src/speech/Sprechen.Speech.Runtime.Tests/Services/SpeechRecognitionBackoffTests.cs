using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechRecognitionBackoffTests
{
    [Fact]
    public void GetDelay_DoublesUpToMax()
    {
        var policy = new ExponentialBackoffPolicy(initialMs: 200, maxMs: 4000, maxRetries: 6);

        Assert.Equal(TimeSpan.FromMilliseconds(200), policy.GetDelay(0));
        Assert.Equal(TimeSpan.FromMilliseconds(400), policy.GetDelay(1));
        Assert.Equal(TimeSpan.FromMilliseconds(800), policy.GetDelay(2));
        Assert.Equal(TimeSpan.FromMilliseconds(1600), policy.GetDelay(3));
        Assert.Equal(TimeSpan.FromMilliseconds(3200), policy.GetDelay(4));
        Assert.Equal(TimeSpan.FromMilliseconds(4000), policy.GetDelay(5));
        Assert.Equal(TimeSpan.FromMilliseconds(4000), policy.GetDelay(6));
        Assert.Equal(TimeSpan.FromMilliseconds(4000), policy.GetDelay(20));
    }

    [Fact]
    public async Task EnqueueAsync_AfterMaxRetriesFailures_ThrowsBackoffExhausted()
    {
        var failingClient = new AlwaysFailingSpeechRecognitionClient();
        var policy = new RecordingBackoffPolicy(maxRetries: 3);
        await using var worker = new SpeechRecognitionWorker(failingClient, policy, maxQueueDepth: 1);

        var task = worker.EnqueueAsync(
            new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]),
            "de",
            CancellationToken.None);

        var ex = await Assert.ThrowsAsync<SpeechRecognitionBackoffExhaustedException>(() => task);
        Assert.Equal("STT_BACKOFF_EXHAUSTED", ex.Code);
        Assert.Equal(SpeechRecognitionBackoffExhaustedException.CodeValue, ex.Code);
        Assert.Equal(3, ex.MaxRetries);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Equal(3, failingClient.CallCount);
        // Wartezeiten fallen nur zwischen den drei Versuchen an, nicht nach dem letzten Fehler.
        Assert.Equal([0, 1], policy.RequestedAttemptIndices);
    }
}
