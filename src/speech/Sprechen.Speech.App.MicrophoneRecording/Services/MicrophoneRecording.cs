using System.Runtime.CompilerServices;
using MicroSharp;
using Microsoft.JSInterop;
using System.Threading.Channels;

namespace Sprechen.Speech.App.MicrophoneRecording.Services;

/// <summary>
/// Verfolgt eine Aufnahme bis zum Stoppen. Eine angehängte Paketsitzung liefert ihre
/// Blöcke direkt; der unbegrenzte Rückrufkanal bleibt auf die explizite Legacy-Erzeugung beschränkt.
/// </summary>
public sealed class MicrophoneRecording : IMicrophoneRecording
{
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

    private readonly object _sync = new();
    private readonly Func<MicrophoneRecording, CancellationToken, Task> _stopAsync;
    private readonly Channel<byte[]>? _legacyChunks;
    private readonly TaskCompletionSource _stopped;
    private readonly DotNetObjectReference<MicrophoneRecording> _dotNetRef;

    private TaskCompletionSource? _stopAttempt;
    private IMicrophoneSession? _captureSession;
    private bool _disposed;

    internal static MicrophoneRecording CreateLegacy(
        Func<MicrophoneRecording, CancellationToken, Task> stopAsync)
        => new(stopAsync, legacyInjection: true);

    internal MicrophoneRecording(Func<MicrophoneRecording, CancellationToken, Task> stopAsync)
        : this(stopAsync, legacyInjection: false)
    {
    }

    private MicrophoneRecording(
        Func<MicrophoneRecording, CancellationToken, Task> stopAsync,
        bool legacyInjection)
    {
        _stopAsync = stopAsync ?? throw new ArgumentNullException(nameof(stopAsync));
        if (legacyInjection)
        {
            _legacyChunks = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
            {
                SingleReader = true
            });
        }

        _stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _dotNetRef = DotNetObjectReference.Create(this);
    }

    public string MimeType { get; private set; } = string.Empty;

    public IAsyncEnumerable<byte[]> Chunks => _captureSession is not null
        ? ReadPackageChunks(_captureSession)
        : ReadLegacyChunks();

    /// <summary>Verworfene Blöcke der Bibliothekswarteschlange. Der Lookback-Ring zählt sie nicht.</summary>
    public long CaptureDroppedChunkCount => _captureSession?.DroppedChunkCount ?? 0;

    internal void AttachCapture(IMicrophoneSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _captureSession = session;
    }

    public Task Completion => _stopped.Task;

    internal DotNetObjectReference<MicrophoneRecording> CallbackReference => _dotNetRef;

    public ValueTask StopAsync()
    {
        return StopAsync(CancellationToken.None);
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource stopAttempt;
        var startsAttempt = false;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            if (_stopAttempt is null)
            {
                _stopAttempt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                startsAttempt = true;
            }

            stopAttempt = _stopAttempt;
        }

        if (startsAttempt)
        {
            _ = CompleteStopAttemptAsync(stopAttempt);
        }

        await stopAttempt.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task CompleteStopAttemptAsync(TaskCompletionSource stopAttempt)
    {
        try
        {
            await _stopAsync(this, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            lock (_sync)
            {
                if (ReferenceEquals(_stopAttempt, stopAttempt))
                {
                    _stopAttempt = null;
                }
            }

            stopAttempt.TrySetException(exception);
            return;
        }

        var completed = await Task.WhenAny(
                _stopped.Task,
                Task.Delay(StopTimeout))
            .ConfigureAwait(false);

        if (completed != _stopped.Task)
        {
            CompleteLegacy(new TimeoutException("Microphone recorder stop timed out."));
            _stopped.TrySetResult();
        }

        stopAttempt.TrySetResult();
    }

    [JSInvokable]
    public void OnChunk(byte[] bytes)
    {
        if (_disposed || _captureSession is not null || _legacyChunks is null || bytes is null || bytes.Length == 0)
        {
            return;
        }

        _legacyChunks.Writer.TryWrite(bytes);
    }

    [JSInvokable]
    public void OnError(string message)
    {
        if (_disposed || _captureSession is not null || _legacyChunks is null)
        {
            return;
        }

        var exception = new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? "Microphone recorder error." : message);
        CompleteLegacy(exception);
        _stopped.TrySetResult();
    }

    [JSInvokable]
    public async Task OnBrowserSessionEnded(string message)
    {
        OnError(message);
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    [JSInvokable]
    public void OnStopped()
    {
        if (_disposed)
        {
            return;
        }

        CompleteStopped();
    }

    internal void SetMimeType(string? mimeType)
    {
        MimeType = mimeType?.Trim() ?? string.Empty;
    }

    internal void StartFailed(Exception exception)
    {
        if (_disposed)
        {
            return;
        }

        CompleteLegacy(exception);
        _stopped.TrySetResult();
        _disposed = true;
        _dotNetRef.Dispose();
    }

    internal void ReleaseCaptureEnded()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        CompleteStopped();
        _dotNetRef.Dispose();
    }

    internal bool IsDisposed
    {
        get
        {
            lock (_sync)
            {
                return _disposed;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }
        }

        await StopAsync().ConfigureAwait(false);

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        CompleteStopped();
        _dotNetRef.Dispose();
        await ReleaseAttachedCaptureAsync().ConfigureAwait(false);
    }

    private async ValueTask ReleaseAttachedCaptureAsync()
    {
        IMicrophoneSession? session;
        lock (_sync)
        {
            session = _captureSession;
            _captureSession = null;
        }

        if (session is not null)
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }
    }

    private void CompleteStopped()
    {
        CompleteLegacy();
        _stopped.TrySetResult();
    }

    private void CompleteLegacy(Exception? error = null)
    {
        if (_legacyChunks is null)
        {
            return;
        }

        if (error is null)
        {
            _legacyChunks.Writer.TryComplete();
        }
        else
        {
            _legacyChunks.Writer.TryComplete(error);
        }
    }

    private IAsyncEnumerable<byte[]> ReadLegacyChunks()
    {
        if (_legacyChunks is null)
        {
            throw new InvalidOperationException("This recording has no capture session.");
        }

        return _legacyChunks.Reader.ReadAllAsync();
    }

    private async IAsyncEnumerable<byte[]> ReadPackageChunks(
        IMicrophoneSession session,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in session.ReadChunksAsync(cancellationToken).ConfigureAwait(false))
        {
            var payload = chunk.Payload;
            if (payload.Length == 0)
            {
                continue;
            }

            yield return payload;
        }
    }
}
