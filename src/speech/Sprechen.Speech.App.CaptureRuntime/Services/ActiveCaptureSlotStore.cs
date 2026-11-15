namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verwaltet den aktiven Aufnahmeversuch und seine Generation, damit ein späteres
/// Abschlusssignal keine inzwischen begonnene Aufnahme betrifft.
/// </summary>
internal sealed class ActiveCaptureSlotStore
{
    private readonly object _gate = new();

    private readonly SpeechSampleRing? _sampleRing;
    private ActiveCaptureSlot? _activeCaptureSlot;
    private long _nextAttemptId;
    private long _activeAttemptId;
    private CaptureAttemptAdmission _currentAdmission = new();
    private PendingCaptureAttemptTerminal? _pendingAttemptTerminal;
    private SampleCursor _nextCursor;

    internal ActiveCaptureSlotStore()
    {
    }

    internal ActiveCaptureSlotStore(SpeechSampleRing sampleRing)
    {
        _sampleRing = sampleRing ?? throw new ArgumentNullException(nameof(sampleRing));
    }

    internal void ResetToLiveEdge()
    {
        lock (_gate)
        {
            if (_sampleRing is null)
            {
                throw new InvalidOperationException("Active capture cursor reset requires a sample ring.");
            }

            _nextCursor = _sampleRing.OpenCursor(SampleCursorOrigin.Latest);
        }
    }

    internal ActiveCaptureSlot Arm(
        PcmSpeechCaptureSegmenter segmenter,
        SpeechCaptureModeBehavior behavior,
        SampleCursor? startCursor = null,
        long attemptId = 0)
    {
        var slot = new ActiveCaptureSlot(segmenter, behavior);
        PendingCaptureAttemptTerminal? pendingTerminal = null;

        lock (_gate)
        {
            if (attemptId != 0 && _activeAttemptId != attemptId)
            {
                throw new InvalidOperationException("The capture attempt is no longer active.");
            }

            if (_sampleRing is not null)
            {
                _nextCursor = startCursor ?? _sampleRing.OpenCursor(SampleCursorOrigin.Latest);
            }

            pendingTerminal = _pendingAttemptTerminal;
            _activeAttemptId = 0;
            _pendingAttemptTerminal = null;
            if (pendingTerminal is null)
            {
                _activeCaptureSlot = slot;
            }
        }

        pendingTerminal?.Complete(slot);

        return slot;
    }

    internal CaptureAttemptAdmission AdmitAttempt()
    {
        lock (_gate)
        {
            return _currentAdmission;
        }
    }

    internal long BeginAttempt(CaptureAttemptAdmission admission)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(admission, _currentAdmission))
            {
                // Die Zulassung merkt das erste Abschlusssignal, das ihre Generation beendet hat.
            }
            else
            {
                if (_activeAttemptId != 0)
                {
                    throw new InvalidOperationException("A capture attempt is already active.");
                }

                _activeAttemptId = ++_nextAttemptId;
                _pendingAttemptTerminal = null;
                return _activeAttemptId;
            }
        }

        admission.ThrowIfInvalid();
        throw new InvalidOperationException("The invalidated capture admission did not throw its terminal.");
    }

    internal void ThrowIfAdmissionInvalid(CaptureAttemptAdmission admission)
    {
        admission.ThrowIfInvalid();
    }

    internal long BeginAttempt()
    {
        var admission = AdmitAttempt();
        return BeginAttempt(admission);
    }

    internal void DisposeAdmissionCancellation()
    {
        lock (_gate)
        {
            _currentAdmission.Dispose();
        }
    }

    private (ActiveCaptureSlot? Slot, CaptureAttemptAdmission AdmittedAttempts) BeginTerminal(
        PendingCaptureAttemptTerminal terminal)
    {
        lock (_gate)
        {
            var admittedAttempts = _currentAdmission;
            _currentAdmission = new CaptureAttemptAdmission();
            admittedAttempts.Invalidate(terminal);

            if (_activeAttemptId != 0)
            {
                _pendingAttemptTerminal ??= terminal;
            }

            return (ClearCore(), admittedAttempts);
        }
    }

    internal void EndAttempt(long attemptId)
    {
        lock (_gate)
        {
            if (_activeAttemptId != attemptId)
            {
                return;
            }

            _activeAttemptId = 0;
            _pendingAttemptTerminal = null;
        }
    }

    internal ActiveCaptureSlot? FailActiveOrPendingAttempt(Exception exception)
    {
        var terminal = PendingCaptureAttemptTerminal.Failure(exception);
        var (activeSlot, admittedAttempts) = BeginTerminal(terminal);
        admittedAttempts.Cancel();
        return activeSlot;
    }

    internal ActiveCaptureSlot? CancelActiveOrPendingAttempt(
        CaptureCancellationKind kind,
        CancellationToken cancellationToken)
    {
        var terminal = PendingCaptureAttemptTerminal.Cancellation(kind, cancellationToken);
        var (activeSlot, admittedAttempts) = BeginTerminal(terminal);
        admittedAttempts.Cancel();
        return activeSlot;
    }

    internal ActiveCaptureChunkResult ProcessChunk(byte[] chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        lock (_gate)
        {
            if (_activeCaptureSlot is null)
            {
                return ActiveCaptureChunkResult.NoActiveCapture;
            }

            if (_sampleRing is null)
            {
                throw new InvalidOperationException("Active capture processing requires a sample ring cursor.");
            }

            var activeSlot = _activeCaptureSlot;
            var nextCursor = _nextCursor;
            var update = activeSlot.ReadFrom(_sampleRing, ref nextCursor);
            _nextCursor = nextCursor;
            var modeBehavior = activeSlot.ModeBehavior;

            if (update.CompletionException is not null)
            {
                return new ActiveCaptureChunkResult(
                    ClearCore(),
                    CompletedUtterance: null,
                    update.CompletionException,
                    update.SpeechStarted,
                    modeBehavior);
            }

            if (update.CompletedUtterance is not null)
            {
                return new ActiveCaptureChunkResult(
                    ClearCore(),
                    update.CompletedUtterance,
                    CompletionException: null,
                    update.SpeechStarted,
                    modeBehavior);
            }

            return new ActiveCaptureChunkResult(
                CompletedSlot: null,
                CompletedUtterance: null,
                CompletionException: null,
                update.SpeechStarted,
                modeBehavior);
        }
    }

    internal ActiveCaptureBacklogReadResult ReadFrom(
        ActiveCaptureSlot expectedSlot)
    {
        ArgumentNullException.ThrowIfNull(expectedSlot);

        lock (_gate)
        {
            if (!ReferenceEquals(_activeCaptureSlot, expectedSlot))
            {
                return ActiveCaptureBacklogReadResult.Inactive;
            }

            if (_sampleRing is null)
            {
                throw new InvalidOperationException("Active capture backlog reading requires a sample ring cursor.");
            }

            var cursorBefore = _nextCursor.SampleIndex;
            var nextCursor = _nextCursor;
            var update = expectedSlot.ReadFrom(_sampleRing, ref nextCursor);
            _nextCursor = nextCursor;
            var lookbackBytes = nextCursor.SampleIndex - cursorBefore;

            return new ActiveCaptureBacklogReadResult(
                IsActive: true,
                update,
                Advanced: lookbackBytes != 0,
                LookbackBytes: lookbackBytes > int.MaxValue ? int.MaxValue : (int)lookbackBytes);
        }
    }

    internal ActiveCaptureSlot? Clear()
    {
        lock (_gate)
        {
            return ClearCore();
        }
    }

    internal ActiveCaptureSlot? Clear(ActiveCaptureSlot expectedSlot)
    {
        lock (_gate)
        {
            return ClearCore(expectedSlot);
        }
    }

    private ActiveCaptureSlot? ClearCore()
    {
        var activeSlot = _activeCaptureSlot;
        _activeCaptureSlot = null;
        return activeSlot;
    }

    private ActiveCaptureSlot? ClearCore(ActiveCaptureSlot expectedSlot)
    {
        if (!ReferenceEquals(_activeCaptureSlot, expectedSlot))
        {
            return null;
        }

        _activeCaptureSlot = null;
        return expectedSlot;
    }
}
