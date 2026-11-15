namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Schließt aktive oder vorbereitete Segmentversuche bei Fehler und Abbruch ab,
/// damit beim Leeren bereits erfasste Teilsprache noch als Ergebnis ausgegeben werden kann.
/// </summary>
internal sealed class ActiveSegmentTerminalSignal
{
    private readonly ActiveSegmentSlotStore _slotStore;

    internal ActiveSegmentTerminalSignal(ActiveSegmentSlotStore slotStore)
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

    internal void CancelSlot(ActiveSegmentSlot activeSlot, CancellationToken cancellationToken)
    {
        var canceledSlot = _slotStore.Clear(activeSlot);

        canceledSlot?.TryCancel(cancellationToken);
    }

    internal void DisposeActiveSegment()
    {
        _slotStore.Clear()?.Dispose();
    }

    private static void CompleteCancellation(
        ActiveSegmentSlot activeSlot,
        CaptureCancellationKind kind,
        CancellationToken cancellationToken)
    {
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
