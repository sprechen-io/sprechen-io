namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verteilt Audioblöcke an den aktiven Aufnahmeweg und zusätzliche Verbraucher.
/// Die Verbraucherliste wird beim Lesen wiederverwendet. Doppelte Anmeldungen werden vermieden.
/// </summary>
internal sealed class SpeechCaptureChunkDispatcher
{
    private readonly SpeechCaptureActiveCaptureCoordinator _activeCaptureCoordinator;
    private readonly object _gate = new();

    // Die Verbraucherliste nur beim An- oder Abmelden kopieren. Audioblöcke lesen
    // den veröffentlichten Stand, ohne dafür ein neues Array anzulegen.
    // Vollständige Arrays werden atomar veröffentlicht und danach nicht verändert.
    private ISpeechCaptureChunkConsumer[] _additionalConsumers = [];

    public SpeechCaptureChunkDispatcher(SpeechCaptureActiveCaptureCoordinator activeCaptureCoordinator)
    {
        _activeCaptureCoordinator = activeCaptureCoordinator ?? throw new ArgumentNullException(nameof(activeCaptureCoordinator));
    }

    public void RegisterAdditionalConsumer(ISpeechCaptureChunkConsumer consumer)
    {
        ArgumentNullException.ThrowIfNull(consumer);

        lock (_gate)
        {
            var current = _additionalConsumers;
            if (Array.IndexOf(current, consumer) >= 0)
            {
                return;
            }

            var next = new ISpeechCaptureChunkConsumer[current.Length + 1];
            current.CopyTo(next, 0);
            next[current.Length] = consumer;
            Volatile.Write(ref _additionalConsumers, next);
        }
    }

    public void UnregisterAdditionalConsumer(ISpeechCaptureChunkConsumer consumer)
    {
        ArgumentNullException.ThrowIfNull(consumer);

        lock (_gate)
        {
            var current = _additionalConsumers;
            var index = Array.IndexOf(current, consumer);
            if (index < 0)
            {
                return;
            }

            if (current.Length == 1)
            {
                Volatile.Write(ref _additionalConsumers, []);
                return;
            }

            var next = new ISpeechCaptureChunkConsumer[current.Length - 1];
            Array.Copy(current, 0, next, 0, index);
            Array.Copy(current, index + 1, next, index, current.Length - index - 1);
            Volatile.Write(ref _additionalConsumers, next);
        }
    }

    public async Task ProcessChunkAsync(byte[] chunk, CancellationToken cancellationToken)
    {
        await _activeCaptureCoordinator.ProcessChunkAsync(chunk, cancellationToken).ConfigureAwait(false);

        foreach (var consumer in Volatile.Read(ref _additionalConsumers))
        {
            await consumer.ProcessChunkAsync(chunk, cancellationToken).ConfigureAwait(false);
        }
    }

    public void FailActiveConsumers(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        _activeCaptureCoordinator.FailActiveCapture(ex);

        foreach (var consumer in Volatile.Read(ref _additionalConsumers))
        {
            consumer.FailActiveCapture(ex);
        }
    }

    public void CancelActiveCaptures(CaptureCancellationKind kind, CancellationToken cancellationToken)
    {
        _activeCaptureCoordinator.CancelActiveCapture(kind, cancellationToken);

        foreach (var consumer in Volatile.Read(ref _additionalConsumers))
        {
            consumer.CancelActiveCapture(kind, cancellationToken);
        }
    }
}
