using System.Diagnostics;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Überwacht das Eintreffen von Audioblöcken während eines Aufnahmeversuchs,
/// damit ein stehengebliebener Datenstrom als technischer Fehler statt als Stille gemeldet wird.
/// </summary>
internal sealed class CaptureChunkLivenessGuard(
    TimeSpan timeout,
    IBrowserDebugConsole browserDebugConsole,
    string stalledMessage)
{
    private readonly TimeSpan _timeout = timeout > TimeSpan.Zero
        ? timeout
        : throw new ArgumentOutOfRangeException(nameof(timeout));
    private readonly IBrowserDebugConsole _browserDebugConsole = browserDebugConsole
        ?? throw new ArgumentNullException(nameof(browserDebugConsole));
    private readonly string _stalledMessage = !string.IsNullOrWhiteSpace(stalledMessage)
        ? stalledMessage
        : throw new ArgumentException("A stalled-capture message is required.", nameof(stalledMessage));
    private long _lastChunkTimestamp;

    internal void ObserveChunk() => Volatile.Write(ref _lastChunkTimestamp, Stopwatch.GetTimestamp());

    internal async Task<CapturedSpeechAudio> AwaitAsync(
        Task<CapturedSpeechAudio> capture,
        Action<Exception> failCapture,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(capture);
        ArgumentNullException.ThrowIfNull(failCapture);

        ObserveChunk();
        var pollInterval = TimeSpan.FromMilliseconds(Math.Clamp(_timeout.TotalMilliseconds / 4d, 10d, 250d));
        while (true)
        {
            var pollDelay = Task.Delay(pollInterval, cancellationToken);
            var finished = await Task.WhenAny(capture, pollDelay).ConfigureAwait(false);
            if (ReferenceEquals(finished, capture))
            {
                return await capture.ConfigureAwait(false);
            }

            await pollDelay.ConfigureAwait(false);
            var expiredTimestamp = Volatile.Read(ref _lastChunkTimestamp);
            if (Stopwatch.GetElapsedTime(expiredTimestamp) < _timeout)
            {
                continue;
            }

            _browserDebugConsole.WriteOptional("speech-capture", _stalledMessage);
            await Task.Delay(pollInterval, cancellationToken).ConfigureAwait(false);
            if (Volatile.Read(ref _lastChunkTimestamp) != expiredTimestamp)
            {
                continue;
            }

            failCapture(new SpeechCaptureStreamStalledException());
            return await capture.ConfigureAwait(false);
        }
    }

}
