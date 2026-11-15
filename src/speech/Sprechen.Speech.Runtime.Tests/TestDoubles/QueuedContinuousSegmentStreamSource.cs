using System.Threading.Channels;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class QueuedContinuousSegmentStreamSource : IContinuousSegmentStreamSource
{
    private readonly Channel<CapturedSpeechAudio> _segments = Channel.CreateUnbounded<CapturedSpeechAudio>(
        new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

    private int _beginListeningCallCount;
    private int _captureNextSegmentCallCount;
    private int _stopListeningCallCount;

    public int BeginListeningCallCount => Volatile.Read(ref _beginListeningCallCount);

    public int CaptureNextSegmentCallCount => Volatile.Read(ref _captureNextSegmentCallCount);

    public int StopListeningCallCount => Volatile.Read(ref _stopListeningCallCount);

    public Exception? BeginListeningFailure { get; set; }

    public Exception? CaptureNextSegmentFailure { get; set; }

    public ValueTask EnqueueSegmentAsync(CapturedSpeechAudio segment)
        => _segments.Writer.WriteAsync(segment, CancellationToken.None);

    public ValueTask BeginListeningAsync(SpeechCaptureMode mode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _beginListeningCallCount);

        if (BeginListeningFailure is { } failure)
        {
            return ValueTask.FromException(failure);
        }

        return ValueTask.CompletedTask;
    }

    public Task<CapturedSpeechAudio> CaptureNextSegmentAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _captureNextSegmentCallCount);

        if (CaptureNextSegmentFailure is { } failure)
        {
            return Task.FromException<CapturedSpeechAudio>(failure);
        }

        return _segments.Reader.ReadAsync(cancellationToken).AsTask();
    }

    public ValueTask StopListeningAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _stopListeningCallCount);
        return ValueTask.CompletedTask;
    }
}
