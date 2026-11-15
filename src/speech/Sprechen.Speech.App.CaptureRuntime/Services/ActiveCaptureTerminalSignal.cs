namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Überführt Fehler und Abbrucharten in den Abschluss des aktuellen Aufnahmeversuchs,
/// damit Pausieren und die Ausgabe bereits erfasster Teilsprache ihre eigene Bedeutung behalten.
/// </summary>
internal sealed class ActiveCaptureTerminalSignal
{
    private readonly ActiveCaptureSlotStore _slotStore;

    internal ActiveCaptureTerminalSignal(ActiveCaptureSlotStore slotStore)
    {
        _slotStore = slotStore ?? throw new ArgumentNullException(nameof(slotStore));
    }

    internal void FailActiveCapture(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        _slotStore.FailActiveOrPendingAttempt(exception)?.TryFail(exception);
    }

    internal void CancelActiveCapture(CaptureCancellationKind kind, CancellationToken cancellationToken)
    {
        var activeSlot = _slotStore.CancelActiveOrPendingAttempt(kind, cancellationToken);

        if (activeSlot is null)
        {
            return;
        }

        CompleteCancellation(activeSlot, kind, cancellationToken);
    }

    internal void CancelSlot(ActiveCaptureSlot activeSlot, CancellationToken cancellationToken)
    {
        var canceledSlot = _slotStore.Clear(activeSlot);

        canceledSlot?.TryCancel(cancellationToken);
    }

    internal void DisposeActiveCapture()
    {
        _slotStore.Clear()?.Dispose();
    }

    private static void CompleteCancellation(
        ActiveCaptureSlot activeSlot,
        CaptureCancellationKind kind,
        CancellationToken cancellationToken)
    {
        if (kind == CaptureCancellationKind.Paused)
        {
            activeSlot.TryFail(new VoiceInputPausedException());
            return;
        }

        if (kind != CaptureCancellationKind.Drain)
        {
            activeSlot.TryCancel(cancellationToken);
            return;
        }

        var partial = activeSlot.FlushPartial();
        if (partial is not null)
        {
            activeSlot.TryComplete(partial);
            return;
        }

        activeSlot.TryCancel(cancellationToken);
    }
}
