namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Serialisiert Aufnahmen beider Aufnahmewege und protokolliert ihren Abschluss.
/// Die Beobachtungen werden vor der Freigabe der Aufnahme für den Bericht übernommen.
/// </summary>
internal sealed class CaptureTerminalFlow : IDisposable
{
    private readonly ActiveCaptureTrace _trace;
    private readonly SemaphoreSlim _captureGate = new(1, 1);

    internal CaptureTerminalFlow(ActiveCaptureTrace trace)
    {
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
    }

    internal async Task<CapturedSpeechAudio> RunAsync(
        Func<Task<CapturedSpeechAudio>> capture,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(capture);

        var captureGateHeld = false;
        try
        {
            await _captureGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            captureGateHeld = true;
            return await capture().ConfigureAwait(false);
        }
        catch (NoSpeechDetectedException)
        {
            await _trace.RecordNoSpeechAsync().ConfigureAwait(false);
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await _trace.RecordCancellationAsync().ConfigureAwait(false);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await _trace.RecordFailureAsync(ex).ConfigureAwait(false);
            throw;
        }
        finally
        {
            // Erst nach dem Abschlussbericht freigeben, damit die nächste Aufnahme
            // den dafür benötigten Beobachtungszustand nicht vorzeitig zurücksetzt.
            if (captureGateHeld)
            {
                _captureGate.Release();
            }
        }
    }

    internal async ValueTask<CapturedSpeechAudio> CompleteAsync(
        IArmedCaptureWindow armedWindow,
        CapturedSpeechAudio captured)
    {
        ArgumentNullException.ThrowIfNull(armedWindow);
        ArgumentNullException.ThrowIfNull(captured);

        // Auch ein noch vorhandenes Teilsegment erhält hier die Kennung
        // seines Aufnahmeversuchs, bevor der Abschlussbericht sie zurücksetzt.
        var utterance = captured with { TurnId = _trace.TurnId };
        ObserveWindow(armedWindow);
        await _trace.RecordCompleteAsync(utterance).ConfigureAwait(false);
        return utterance;
    }

    internal void Release<TWindow>(TWindow? armedWindow, Action<TWindow> clearWindow)
        where TWindow : class, IArmedCaptureWindow
    {
        ArgumentNullException.ThrowIfNull(clearWindow);

        if (armedWindow is null)
        {
            return;
        }

        // Die Messwerte vor dem Freigeben übernehmen, damit sie für den späteren Fehlerbericht verfügbar sind.
        ObserveWindow(armedWindow);
        armedWindow.Dispose();
        clearWindow(armedWindow);
    }

    public void Dispose()
    {
        _captureGate.Dispose();
    }

    private void ObserveWindow(IArmedCaptureWindow armedWindow)
    {
        _trace.ObservePeakAmplitude(armedWindow.MaxAmplitudeObserved);
        _trace.ObserveArrivedFrames(armedWindow.ObservedFrameCount);
        _trace.ObserveOnsetDecision(armedWindow.EstimatedFloor, armedWindow.AppliedOnsetThreshold);
    }
}
