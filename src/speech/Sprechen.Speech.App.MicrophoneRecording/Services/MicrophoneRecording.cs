using Microsoft.JSInterop;
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

    public IAsyncEnumerable<byte[]> Chunks => _chunks.Reader.ReadAllAsync();

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
    }

    private void CompleteStopped()
    {
        _chunks.Writer.TryComplete();
        _stopped.TrySetResult();
    }
}
