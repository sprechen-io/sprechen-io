using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Protokolliert den Abschluss eines Aufnahmeversuchs und erstellt den zugehörigen Bericht,
/// damit beide dieselben Beobachtungen und dieselbe Kennung verwenden.
/// </summary>
internal sealed class ActiveCaptureTrace
{
    private readonly IBrowserDebugConsole _browserDebugConsole;
    private readonly ILogger _logger;
    private readonly ICaptureDiagnosticsReporter _diagnostics;
    private readonly ActiveCaptureAttemptObservationState _observations = new();

    private static int _captureSeq;

    // Für jeden Aufnahmebeginn entsteht eine neue Kennung, damit Audiodaten und Bericht
    // derselben Aufnahme der dienstseitigen Erkennung zugeordnet werden können.
    internal string? TurnId { get; private set; }

    internal ActiveCaptureTrace(
        IBrowserDebugConsole browserDebugConsole,
        ILogger logger,
        ICaptureDiagnosticsReporter? diagnostics = null)
    {
        _browserDebugConsole = browserDebugConsole ?? throw new ArgumentNullException(nameof(browserDebugConsole));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _diagnostics = diagnostics ?? new NoOpCaptureDiagnosticsReporter();
    }

    // Die Zahl der Audioblöcke beschreibt den Umfang des erfassten Materials,
    // nicht die Dauer der dienstseitigen Verarbeitung.
    private static int CapturedChunksOf(CapturedSpeechAudio utterance)
        => utterance.Chunks.Count;

    internal void ObservePeakAmplitude(int peakAmplitude)
        => _observations.ObservePeakAmplitude(peakAmplitude);

    internal void ObserveOnsetDecision(int estimatedFloor, int appliedOnsetThreshold)
        => _observations.ObserveOnsetDecision(estimatedFloor, appliedOnsetThreshold);

    internal void ObserveArrivedFrames(int observedFrames)
        => _observations.ObserveArrivedFrames(observedFrames);

    internal void ObserveLookbackBytes(int lookbackBytes)
        => _observations.ObserveLookbackBytes(lookbackBytes);

    private ValueTask ReportAsync(string outcome, int capturedChunks, string? faultKind = null)
    {
        // Optionale Diagnoseberichte werden ohne Warten auf ihren Versand ausgelöst.
        try
        {
            var report = _diagnostics.ReportAsync(
                new CaptureDiagnosticsReport(
                    Interlocked.Increment(ref _captureSeq),
                    outcome,
                    _observations.ResolveArmedToOutcomeMs(),
                    _observations.CaptureMode.ToString(),
                    _observations.StartContext.ToString(),
                    capturedChunks,
                    _observations.PeakAmplitude,
                    faultKind,
                    _observations.ToWindowObservation(),
                    _observations.EstimatedFloor,
                    _observations.AppliedOnsetThreshold,
                    TurnId),
                CancellationToken.None);
            if (!report.IsCompletedSuccessfully)
            {
                _ = ObserveReportAsync(report);
            }
        }
        catch
        {
            // Ein Diagnosefehler darf den Aufnahmeabschluss nicht verhindern.
        }

        // Nach dem Abschluss wird die Kennung gelöscht, damit ein früher Fehler
        // des nächsten Versuchs nicht die Kennung der vorigen Aufnahme übernimmt.
        TurnId = null;
        return ValueTask.CompletedTask;
    }

    private static async Task ObserveReportAsync(ValueTask report)
    {
        try
        {
            await report.ConfigureAwait(false);
        }
        catch
        {
            // Optionale Diagnosefehler beobachten und verwerfen.
        }
    }

    internal ValueTask RecordStartAsync(
        int silenceDebounceMs,
        SpeechCaptureStartContext captureStartContext,
        SpeechCaptureModeBehavior behavior)
    {
        _observations.MarkStarted(behavior.Mode, captureStartContext);
        _browserDebugConsole.WriteOptional(
            "speech-capture",
            $"record start mode={behavior.Mode} silenceDebounceMs={silenceDebounceMs} startContext={captureStartContext} bargeInEnabled={behavior.BargeInEnabled}");
        return ValueTask.CompletedTask;
    }

    internal ValueTask RecordCursorHandoffAsync(SampleCursor? startCursor)
    {
        _observations.MarkCursorHandoff(startCursor is not null);
        if (startCursor is { } startCursorTrace)
        {
            _browserDebugConsole.WriteOptional(
                "speech-capture",
                $"cursor handoff sampleIndex={startCursorTrace.SampleIndex}");
        }

        return ValueTask.CompletedTask;
    }

    internal ValueTask RecordArmedAsync()
    {
        _observations.MarkArmed();
        TurnId = Guid.NewGuid().ToString("N");
        // Protokollzeile und Bericht lesen denselben Zustand, damit sie
        // Modus und Startkontext desselben Versuchs wiedergeben.
        _browserDebugConsole.WriteOptional(
            "speech-capture",
            $"capture armed mode={_observations.CaptureMode} startContext={_observations.StartContext}");
        return ValueTask.CompletedTask;
    }

    internal async ValueTask RecordCompleteAsync(CapturedSpeechAudio utterance)
    {
        ArgumentNullException.ThrowIfNull(utterance);

        _browserDebugConsole.WriteOptional(
            "speech-capture",
            $"record complete chunks={utterance.Chunks.Count} mimeType={utterance.MimeType}");
        await ReportAsync("speech", CapturedChunksOf(utterance)).ConfigureAwait(false);
    }

    internal async ValueTask RecordNoSpeechAsync()
    {
        _browserDebugConsole.WriteOptional(
            "speech-capture",
            "record no-speech-detected");
        await ReportAsync(_observations.ResolveNoSpeechOutcome(), _observations.ObservedFrames).ConfigureAwait(false);
    }

    internal async ValueTask RecordCancellationAsync()
    {
        _browserDebugConsole.WriteOptional(
            "speech-capture",
            "record canceled by caller");
        await ReportAsync("cancelled", _observations.ObservedFrames).ConfigureAwait(false);
    }

    internal async ValueTask RecordFailureAsync(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        _logger.LogDebug(exception, "Capturing microphone utterance failed.");
        _browserDebugConsole.WriteOptional(
            "speech-capture",
            $"record failed {exception.GetType().Name}: {exception.Message}");
        // Der Bericht überträgt den Ausnahmetyp, damit die Fehlerart auch
        // ohne Zugriff auf die Browserkonsole am Server erkennbar bleibt.
        await ReportAsync("failed", _observations.ObservedFrames, exception.GetType().Name).ConfigureAwait(false);
    }
}
