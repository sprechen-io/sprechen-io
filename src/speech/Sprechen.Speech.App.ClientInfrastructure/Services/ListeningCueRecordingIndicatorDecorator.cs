using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Ergänzt den Aufnahmeindikator um hörbare Bereitschafts- und Verarbeitungssignale
/// und protokolliert Wiedergabefehler, ohne dadurch die Aufnahme abzubrechen.
/// </summary>
public sealed class ListeningCueRecordingIndicatorDecorator : ISpeechCaptureRecordingIndicator
{
    private readonly ISpeechCaptureRecordingIndicator _inner;
    private readonly IEarconPlayer _earconPlayer;
    private readonly ILogger<ListeningCueRecordingIndicatorDecorator> _logger;

    public ListeningCueRecordingIndicatorDecorator(
        ISpeechCaptureRecordingIndicator inner,
        IEarconPlayer earconPlayer,
        ILogger<ListeningCueRecordingIndicatorDecorator> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _earconPlayer = earconPlayer ?? throw new ArgumentNullException(nameof(earconPlayer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsRecording => _inner.IsRecording;

    public async ValueTask<IAsyncDisposable> BeginRecordingAsync()
    {
        // Den Bereitschaftston im Hintergrund starten, damit eine schnelle Antwort während
        // des Tons den Aufnahmebeginn nicht durch ein zusätzliches Warten hier verpasst.
        // Die Aufnahme verwaltet ihren Rückblick und das Abwarten laufender Wiedergabe
        // selbst. Dieser Indikator wartet nur auf den inneren Aufnahmeanspruch.
        _ = PlayListeningCueInBackgroundAsync();

        return await _inner.BeginRecordingAsync().ConfigureAwait(false);
    }

    private async Task PlayListeningCueInBackgroundAsync()
    {
        try
        {
            await _earconPlayer.PlayAsync(EarconId.FlowListening, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Ein fehlender Bereitschaftston nimmt blinden Nutzern die hörbare Orientierung.
            // Den Fehler deshalb protokollieren, ohne die Aufnahme durch Weiterwerfen abzubrechen.
            _logger.LogWarning(ex, "Listening cue playback failed: {Earcon}.", EarconId.FlowListening);
        }
    }

    public async ValueTask PlayInputAcceptedCueAsync(CancellationToken cancellationToken)
    {
        await PlayCueSafeAsync(EarconId.FlowAck, cancellationToken).ConfigureAwait(false);
        await PlayCueSafeAsync(EarconId.FlowProcessing, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask StopProcessingCueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _earconPlayer.StopLoopingAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Ein nicht gestoppter Verarbeitungston kann die nächste Ansage überlagern.
            // Den Fehler protokollieren, ohne dadurch den laufenden Dialogzug abzubrechen.
            _logger.LogWarning(ex, "Stopping the looping processing cue failed.");
        }
    }

    public ValueTask EnsureStoppedAsync()
        => _inner.EnsureStoppedAsync();

    public IDisposable SubscribeToRecordingStateChanged(
        Func<SpeechAssistantRecordingStateChanged, ValueTask> handler)
        => _inner.SubscribeToRecordingStateChanged(handler);

    private async ValueTask PlayCueSafeAsync(EarconId earconId, CancellationToken cancellationToken)
    {
        try
        {
            await _earconPlayer.PlayAsync(earconId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Earcon playback failed: {Earcon}.", earconId);
        }
    }
}
