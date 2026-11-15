namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Bewahrt Fehler oder Abbruch eines noch nicht aktivierten Aufnahmeversuchs,
/// damit dessen späterer Aufnahmeplatz keinen bereits beendeten Versuch wiederbelebt.
/// </summary>
internal sealed class PendingCaptureAttemptTerminal
{
    private readonly Exception? _failure;
    private readonly CaptureCancellationKind _cancellationKind;
    private readonly CancellationToken _cancellationToken;

    private PendingCaptureAttemptTerminal(Exception failure)
    {
        _failure = failure ?? throw new ArgumentNullException(nameof(failure));
    }

    private PendingCaptureAttemptTerminal(
        CaptureCancellationKind cancellationKind,
        CancellationToken cancellationToken)
    {
        _cancellationKind = cancellationKind;
        _cancellationToken = cancellationToken;
    }

    internal static PendingCaptureAttemptTerminal Failure(Exception failure) => new(failure);

    internal static PendingCaptureAttemptTerminal Cancellation(
        CaptureCancellationKind cancellationKind,
        CancellationToken cancellationToken) => new(cancellationKind, cancellationToken);

    internal void Complete(ActiveCaptureSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        if (_failure is not null)
        {
            slot.TryFail(_failure);
            return;
        }

        if (_cancellationKind == CaptureCancellationKind.Paused)
        {
            slot.TryFail(new VoiceInputPausedException());
            return;
        }

        if (_cancellationKind == CaptureCancellationKind.Drain)
        {
            var partial = slot.FlushPartial();
            if (partial is not null)
            {
                slot.TryComplete(partial);
                return;
            }
        }

        slot.TryCancel(_cancellationToken);
    }

    internal void Complete(ActiveSegmentSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        if (_failure is not null)
        {
            slot.TryFail(_failure);
            return;
        }

        if (_cancellationKind == CaptureCancellationKind.Drain)
        {
            var partial = slot.FlushPartial();
            if (partial is not null)
            {
                slot.TryComplete(partial);
                return;
            }
        }

        slot.TryCancel(_cancellationToken);
    }

    internal void Throw()
    {
        if (_failure is not null)
        {
            throw _failure;
        }

        if (_cancellationKind == CaptureCancellationKind.Paused)
        {
            throw new VoiceInputPausedException();
        }

        throw new OperationCanceledException(_cancellationToken);
    }
}
