namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class RecordingPcmAudioPlayer : IPcmAudioPlayer
{
    private int _warmupCount;

    public int WarmupCount => Volatile.Read(ref _warmupCount);

    public ValueTask WarmupAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _warmupCount);
        return ValueTask.CompletedTask;
    }

    public ValueTask DrainAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask SetMakeupAsync(double makeup, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask EnqueuePcm16LeAsync(byte[] pcm16Le, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public Task InitializeAsync(int sampleRate, CancellationToken cancellationToken) => Task.CompletedTask;

    public ValueTask ResetAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
