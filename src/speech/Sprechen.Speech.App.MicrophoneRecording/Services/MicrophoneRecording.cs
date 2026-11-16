using MicroSharp;
using Microsoft.JSInterop;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Sprechen.Speech.App.MicrophoneRecording.Services;

/// <summary>
/// Verbindet Browserrückrufe mit einem asynchronen Audiokanal und dessen Abschluss,
/// damit Aufrufer eine Aufnahme einschließlich Fehlern und Stoppen gemeinsam verfolgen können.
/// </summary>
public sealed class MicrophoneRecording : IMicrophoneRecording
{
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

    private readonly object _sync = new();
    private readonly Func<MicrophoneRecording, CancellationToken, Task> _stopAsync;
    private readonly Channel<byte[]> _chunks;
    private readonly TaskCompletionSource _stopped;
    private readonly DotNetObjectReference<MicrophoneRecording> _dotNetRef;

    private TaskCompletionSource? _stopAttempt;
    private IMicrophoneSession? _captureSession;
    private bool _disposed;

    internal MicrophoneRecording(Func<MicrophoneRecording, CancellationToken, Task> stopAsync)
    {
        _stopAsync = stopAsync ?? throw new ArgumentNullException(nameof(stopAsync));
        _chunks = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
        {
            SingleReader = true
        });
        _stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _dotNetRef = DotNetObjectReference.Create(this);
    }

    public string MimeType { get; private set; } = string.Empty;

    public IAsyncEnumerable<byte[]> Chunks => _captureSession is null
        ? _chunks.Reader.ReadAllAsync()
        : ReadCapturedChunks();

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
            _chunks.Writer.TryComplete(new TimeoutException("Microphone recorder stop timed out."));
            _stopped.TrySetResult();
        }

        stopAttempt.TrySetResult();
    }

    [JSInvokable]
    public void OnChunk(byte[] bytes)
    {
        if (_disposed || bytes is null || bytes.Length == 0)
        {
            return;
        }

        _chunks.Writer.TryWrite(bytes);
    }

    [JSInvokable]
    public void OnError(string message)
    {
        if (_disposed)
        {
            return;
        }

        var exception = new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? "Microphone recorder error." : message);
        _chunks.Writer.TryComplete(exception);
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

        _chunks.Writer.TryComplete(exception);
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
        _chunks.Writer.TryComplete();
        _stopped.TrySetResult();
    }

    private async IAsyncEnumerable<byte[]> ReadCapturedChunks(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var session = _captureSession ?? throw new InvalidOperationException("The capture session is missing.");
        using var sessionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var injectedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await using var injected = _chunks.Reader.ReadAllAsync(injectedCancellation.Token)
            .GetAsyncEnumerator(injectedCancellation.Token);
        await using var captured = session.ReadChunksAsync(sessionCancellation.Token)
            .GetAsyncEnumerator(sessionCancellation.Token);
        var injectedMove = injected.MoveNextAsync().AsTask();
        var capturedMove = captured.MoveNextAsync().AsTask();
        var injectedDone = false;
        var capturedDone = false;

        while (!injectedDone || !capturedDone)
        {
            var completed = injectedDone
                ? capturedMove
                : capturedDone
                    ? injectedMove
                    : await Task.WhenAny(injectedMove, capturedMove).ConfigureAwait(false);

            if (!injectedDone && ReferenceEquals(completed, injectedMove))
            {
                bool hasInjected;
                try
                {
                    hasInjected = await injectedMove.ConfigureAwait(false);
                }
                catch (Exception)
                {
                    injectedDone = true;
                    await FinishMoveAsync(sessionCancellation, capturedMove, () => capturedDone = true).ConfigureAwait(false);
                    throw;
                }

                if (!hasInjected)
                {
                    injectedDone = true;
                    if (!capturedDone && session.State == MicrophoneSessionState.Running)
                    {
                        await FinishMoveAsync(sessionCancellation, capturedMove, () => capturedDone = true).ConfigureAwait(false);
                    }

                    continue;
                }

                yield return injected.Current;
                injectedMove = injected.MoveNextAsync().AsTask();
                continue;
            }

            if (capturedDone)
            {
                continue;
            }

            bool hasCaptured;
            try
            {
                hasCaptured = await capturedMove.ConfigureAwait(false);
            }
            catch (Exception)
            {
                capturedDone = true;
                await FinishMoveAsync(injectedCancellation, injectedMove, () => injectedDone = true).ConfigureAwait(false);
                throw;
            }

            if (!hasCaptured)
            {
                capturedDone = true;
                continue;
            }

            var payload = captured.Current.Payload;
            if (payload.Length > 0)
            {
                yield return payload;
            }

            capturedMove = captured.MoveNextAsync().AsTask();
        }
    }

    private static async Task FinishMoveAsync(
        CancellationTokenSource cancellation,
        Task<bool> move,
        Action markDone)
    {
        try
        {
            await cancellation.CancelAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
        }

        try
        {
            await move.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
        }

        markDone();
    }
}
