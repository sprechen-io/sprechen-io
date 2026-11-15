using System.Threading.Channels;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Leitet Audio-Chunks einer lokalen Sitzung an einen Kanal weiter.
/// Beim Beenden wird der Kanal abgeschlossen und die Sitzungsnutzung freigegeben.
/// </summary>
internal sealed class SpeechCaptureLocalSessionChunkSource : ISpeechAudioChunkSource, ISpeechCaptureChunkConsumer
{
    private readonly SpeechCaptureChunkDispatcher _chunkDispatcher;
    private readonly SpeechCaptureLocalSessionLease _lease;
    private readonly Channel<byte[]> _chunks = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });
    private readonly object _gate = new();

    private Task? _stopTask;

    public SpeechCaptureLocalSessionChunkSource(
        SpeechCaptureChunkDispatcher chunkDispatcher,
        SpeechCaptureLocalSessionLease lease)
    {
        _chunkDispatcher = chunkDispatcher ?? throw new ArgumentNullException(nameof(chunkDispatcher));
        _lease = lease ?? throw new ArgumentNullException(nameof(lease));
        MimeType = lease.MimeType;

        _chunkDispatcher.RegisterAdditionalConsumer(this);
    }

    public SpeechAudioMimeType MimeType { get; }

    public IAsyncEnumerable<byte[]> Chunks => _chunks.Reader.ReadAllAsync();

    public Task ProcessChunkAsync(byte[] chunk, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        _chunks.Writer.TryWrite(chunk);
        return Task.CompletedTask;
    }

    public void FailActiveCapture(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        ObserveCallbackCompletion(CompleteOnceAsync(ex));
    }

    public void CancelActiveCapture(CaptureCancellationKind kind, CancellationToken cancellationToken)
    {
        ObserveCallbackCompletion(CompleteOnceAsync(completionException: null));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return CompleteOnceAsync(completionException: null).WaitAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(CompleteOnceAsync(completionException: null));
    }

    private Task CompleteOnceAsync(Exception? completionException)
    {
        lock (_gate)
        {
            _stopTask ??= CompleteCoreAsync(completionException);
            return _stopTask;
        }
    }

    private async Task CompleteCoreAsync(Exception? completionException)
    {
        _chunkDispatcher.UnregisterAdditionalConsumer(this);

        if (completionException is null)
        {
            _chunks.Writer.TryComplete();
        }
        else
        {
            _chunks.Writer.TryComplete(completionException);
        }

        await _lease.DisposeAsync().ConfigureAwait(false);
    }

    private static void ObserveCallbackCompletion(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.IsCompletedSuccessfully)
        {
            return;
        }

        // Diese Callbacks geben keine Task zurück. Fehler beim asynchronen
        // Aufräumen deshalb über eine Continuation beobachten.
        _ = task.ContinueWith(
            static completed => _ = completed.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
