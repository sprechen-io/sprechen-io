using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechRecognitionWorkerTests
{
    [Fact]
    public async Task EnqueueAsync_WhenQueueFull_ThrowsWithStableCode()
    {
        // Den ersten Auftrag anhalten, damit weitere Aufträge die Warteschlange kontrolliert füllen.
        var slowClient = new SlowSpeechRecognitionClient();
        await using var worker = new SpeechRecognitionWorker(
            slowClient,
            backoff: new ExponentialBackoffPolicy(initialMs: 1, maxMs: 1, maxRetries: 1),
            maxQueueDepth: 1);

        var firstTask = worker.EnqueueAsync(EmptyAudio(), "de", CancellationToken.None);
        await slowClient.WaitForFirstCallAsync(TimeSpan.FromSeconds(2));

        // Der zweite Auftrag belegt den einzigen Warteplatz, solange der erste noch läuft.
        var secondTask = worker.EnqueueAsync(EmptyAudio(), "de", CancellationToken.None);

        var ex = Assert.Throws<SpeechRecognitionQueueFullException>(
            () => { _ = worker.EnqueueAsync(EmptyAudio(), "de", CancellationToken.None); });

        Assert.Equal("STT_QUEUE_FULL", ex.Code);
        Assert.Equal(SpeechRecognitionQueueFullException.CodeValue, ex.Code);
        Assert.Equal(1, ex.QueueDepth);

        slowClient.ReleaseAll();
        await firstTask;
        await secondTask;
    }

    [Fact]
    public async Task EnqueueAsync_WhenRequestCancelled_CancelsInFlightInnerCall()
    {
        var slowClient = new SlowSpeechRecognitionClient();
        await using var worker = new SpeechRecognitionWorker(slowClient);

        using var cts = new CancellationTokenSource();
        var task = worker.EnqueueAsync(EmptyAudio(), "de", cts.Token);

        await slowClient.WaitForFirstCallAsync(TimeSpan.FromSeconds(2));

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.True(slowClient.LastCallObservedCancellation, "Inner call must observe the per-request cancellation token.");
    }

    [Fact]
    public async Task EnqueueAsync_WhenTheFarSideCancels_CompletesTheRequestAndKeepsServingTheNext()
    {
        // Der Erkenner bricht ab, obwohl weder Auftrag noch Worker beendet wurden.
        var client = new FarSideCancellingRecognitionClient();
        await using var worker = new SpeechRecognitionWorker(
            client,
            backoff: new ExponentialBackoffPolicy(initialMs: 1, maxMs: 1, maxRetries: 1),
            maxQueueDepth: 1);

        var firstAttempt = Record.ExceptionAsync(() =>
            worker.EnqueueAsync(EmptyAudio(), "de", CancellationToken.None));

        // Das Timeout macht auch einen ausbleibenden Abschluss zu einem begrenzten Testfehler.
        var completed = await Task.WhenAny(firstAttempt, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(firstAttempt, completed);
        Assert.IsAssignableFrom<OperationCanceledException>(await firstAttempt);

        // Nach dem fremden Abbruch muss der Worker den nächsten Auftrag weiter bearbeiten.
        client.StopCancelling();
        var second = worker.EnqueueAsync(EmptyAudio(), "de", CancellationToken.None);
        var secondCompleted = await Task.WhenAny(second, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(second, secondCompleted);
        Assert.Equal("nach dem Abbruch", (await second).Text);
    }

    private static CapturedSpeechAudio EmptyAudio() =>
        new(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);
}
