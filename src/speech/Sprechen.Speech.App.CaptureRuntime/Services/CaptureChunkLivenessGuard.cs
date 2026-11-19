using MicroSharp;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Übersetzt den Stillstand der Blocklieferung in den technischen Fehler des Aufnahmeversuchs.
/// </summary>
internal sealed class CaptureChunkLivenessGuard(
    TimeSpan timeout,
    IBrowserDebugConsole browserDebugConsole,
    string stalledMessage,
    TimeProvider? timeProvider = null)
{
    private readonly TimeSpan _timeout = timeout > TimeSpan.Zero
        ? timeout
        : throw new ArgumentOutOfRangeException(nameof(timeout));
    private readonly IBrowserDebugConsole _browserDebugConsole = browserDebugConsole
        ?? throw new ArgumentNullException(nameof(browserDebugConsole));
    private readonly string _stalledMessage = !string.IsNullOrWhiteSpace(stalledMessage)
        ? stalledMessage
        : throw new ArgumentException("A stalled-capture message is required.", nameof(stalledMessage));
    private CaptureLivenessMonitor? _activeMonitor;

    internal void ObserveChunk() => Volatile.Read(ref _activeMonitor)?.ObserveBlock();

    internal async Task<CapturedSpeechAudio> AwaitAsync(
        Task<CapturedSpeechAudio> capture,
        Func<Exception, bool> tryFailCapture,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(capture);
        ArgumentNullException.ThrowIfNull(tryFailCapture);

        if (capture.IsCompleted)
        {
            return await capture.ConfigureAwait(false);
        }

        var monitor = new CaptureLivenessMonitor(_timeout, _timeout, timeProvider);
        if (Interlocked.CompareExchange(ref _activeMonitor, monitor, null) is not null)
        {
            throw new InvalidOperationException("A capture is already being monitored.");
        }

        try
        {
            var stall = monitor.WaitForStallAsync(cancellationToken);
            await Task.WhenAny(capture, stall).ConfigureAwait(false);
            if (!capture.IsCompleted)
            {
                var outcome = await stall.ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (outcome is CaptureLivenessOutcome.StartupTimeout or CaptureLivenessOutcome.InterBlockTimeout
                    && tryFailCapture(new SpeechCaptureStreamStalledException()))
                {
                    _browserDebugConsole.WriteOptional("speech-capture", _stalledMessage);
                }
            }

            return await capture.ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref _activeMonitor, null);
            monitor.Complete();
        }
    }
}
