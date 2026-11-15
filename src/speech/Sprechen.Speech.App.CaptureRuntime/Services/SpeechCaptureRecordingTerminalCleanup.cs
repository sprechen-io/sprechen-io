using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Bündelt Freigabe von Aufnahme und Sitzungstoken nach Fehler oder Ende,
/// damit Fehler der Aufnahmefreigabe den ursprünglichen Aufnahmeabschluss nicht verdecken.
/// </summary>
internal sealed class SpeechCaptureRecordingTerminalCleanup
{
    private readonly ILogger _logger;

    internal SpeechCaptureRecordingTerminalCleanup(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal async Task DisposeStartFailureAsync(
        CancellationTokenSource sessionCts,
        IMicrophoneRecording? recording)
    {
        ArgumentNullException.ThrowIfNull(sessionCts);

        sessionCts.Dispose();

        if (recording is not null)
        {
            await DisposeRecordingAsync(recording, logMessage: null).ConfigureAwait(false);
        }
    }

    internal async Task ResetCompletedSessionAsync(SpeechCaptureSessionSnapshot snapshot)
    {
        CancelAndDisposeToken(snapshot.SessionCts);

        if (snapshot.Recording is not null)
        {
            await DisposeRecordingAsync(snapshot.Recording, "Disposing stale microphone session failed.")
                .ConfigureAwait(false);
        }
    }

    internal async Task StopRecordingAsync(SpeechCaptureSessionSnapshot snapshot)
    {
        CancelAndDisposeToken(snapshot.SessionCts);

        if (snapshot.Recording is not null)
        {
            await DisposeRecordingAsync(snapshot.Recording, "Stopping microphone recording failed.")
                .ConfigureAwait(false);
        }
    }

    internal async Task WaitForShutdownAsync(Task? sessionTask, CancellationToken cancellationToken)
    {
        if (sessionTask is null)
        {
            return;
        }

        try
        {
            await sessionTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Waiting for microphone session shutdown failed.");
        }
    }

    private static void CancelAndDisposeToken(CancellationTokenSource? sessionCts)
    {
        try
        {
            sessionCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            sessionCts?.Dispose();
        }
    }

    private async Task DisposeRecordingAsync(IMicrophoneRecording recording, string? logMessage)
    {
        try
        {
            await recording.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (logMessage is not null)
            {
                _logger.LogDebug(ex, logMessage);
            }
        }
    }
}
