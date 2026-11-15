namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verwaltet aktiven Segmentplatz, Ringpufferposition und ausstehende Abschlusssignale,
/// damit ein Fehler vor Aufnahmebereitschaft beim späteren Aktivieren erhalten bleibt.
/// </summary>
internal sealed class ActiveSegmentSlotStore
{
    private readonly object _gate = new();

    private ActiveSegmentSlot? _activeSegmentSlot;
    private long _nextAttemptId;
    private long _activeAttemptId;
    private PendingCaptureAttemptTerminal? _pendingAttemptTerminal;
    private SpeechSampleRing? _sampleRing;
    private SampleCursor _nextCursor;

    internal void ResetToLiveEdge(SpeechSampleRing sampleRing)
    {
        ArgumentNullException.ThrowIfNull(sampleRing);

        lock (_gate)
        {
            _sampleRing = sampleRing;
            _nextCursor = sampleRing.OpenCursor(SampleCursorOrigin.Latest);
        }
    }

    internal ActiveSegmentSlot Arm(PcmSpeechCaptureSegmenter segmenter, long attemptId = 0)
    {
        var slot = new ActiveSegmentSlot(segmenter);
        PendingCaptureAttemptTerminal? pendingTerminal = null;

        lock (_gate)
        {
            if (attemptId != 0 && _activeAttemptId != attemptId)
            {
                throw new InvalidOperationException("The segment capture attempt is no longer active.");
            }

            pendingTerminal = _pendingAttemptTerminal;
            _activeAttemptId = 0;
            _pendingAttemptTerminal = null;
            if (pendingTerminal is null)
            {
                _activeSegmentSlot = slot;
            }
        }

        pendingTerminal?.Complete(slot);

        return slot;
    }

    internal long BeginAttempt()
    {
        lock (_gate)
        {
            if (_activeAttemptId != 0)
            {
                throw new InvalidOperationException("A segment capture attempt is already active.");
            }

            _activeAttemptId = ++_nextAttemptId;
            _pendingAttemptTerminal = null;
            return _activeAttemptId;
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

    internal ActiveSegmentSlot? FailActiveOrPendingAttempt(Exception exception)
    {
        lock (_gate)
        {
            var activeSlot = ClearCore();
            if (activeSlot is null && _activeAttemptId != 0)
            {
                _pendingAttemptTerminal ??= PendingCaptureAttemptTerminal.Failure(exception);
            }

            return activeSlot;
        }
    }

    internal ActiveSegmentSlot? CancelActiveOrPendingAttempt(
        CaptureCancellationKind kind,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var activeSlot = ClearCore();
            if (activeSlot is null && _activeAttemptId != 0)
            {
                _pendingAttemptTerminal ??= PendingCaptureAttemptTerminal.Cancellation(kind, cancellationToken);
            }

            return activeSlot;
        }
    }

    internal ActiveSegmentChunkResult ProcessChunk(byte[] chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        lock (_gate)
        {
            if (_activeSegmentSlot is null)
            {
                return ActiveSegmentChunkResult.NoActiveSegment;
            }

            if (_sampleRing is null)
            {
                throw new InvalidOperationException("Active segment processing requires a sample ring cursor.");
            }

            var activeSlot = _activeSegmentSlot;
            var nextCursor = _nextCursor;
            var update = activeSlot.ReadFrom(_sampleRing, ref nextCursor);
            _nextCursor = nextCursor;

            if (update.CompletionException is not null)
            {
                return new ActiveSegmentChunkResult(
                    ClearCore(),
                    CompletedUtterance: null,
                    update.CompletionException,
                    update.SpeechStarted);
            }

            if (update.CompletedUtterance is not null)
            {
                return new ActiveSegmentChunkResult(
                    ClearCore(),
                    update.CompletedUtterance,
                    CompletionException: null,
                    update.SpeechStarted);
            }

            return new ActiveSegmentChunkResult(
                CompletedSlot: null,
                CompletedUtterance: null,
                CompletionException: null,
                update.SpeechStarted);
        }
    }

    internal ActiveSegmentBacklogReadResult ReadFrom(
        ActiveSegmentSlot expectedSlot)
    {
        ArgumentNullException.ThrowIfNull(expectedSlot);

        lock (_gate)
        {
            if (!ReferenceEquals(_activeSegmentSlot, expectedSlot))
            {
                return ActiveSegmentBacklogReadResult.Inactive;
            }

            if (_sampleRing is null)
            {
                throw new InvalidOperationException("Active segment backlog reading requires a sample ring cursor.");
            }

            var cursorBefore = _nextCursor.SampleIndex;
            var nextCursor = _nextCursor;
            var update = expectedSlot.ReadFrom(_sampleRing, ref nextCursor);
            _nextCursor = nextCursor;

            return new ActiveSegmentBacklogReadResult(
                IsActive: true,
                update,
                Advanced: nextCursor.SampleIndex != cursorBefore);
        }
    }

    internal ActiveSegmentSlot? Clear()
    {
        lock (_gate)
        {
            return ClearCore();
        }
    }

    internal ActiveSegmentSlot? Clear(ActiveSegmentSlot expectedSlot)
    {
        lock (_gate)
        {
            return ClearCore(expectedSlot);
        }
    }

    private ActiveSegmentSlot? ClearCore()
    {
        var activeSlot = _activeSegmentSlot;
        _activeSegmentSlot = null;
        return activeSlot;
    }

    private ActiveSegmentSlot? ClearCore(ActiveSegmentSlot expectedSlot)
    {
        if (!ReferenceEquals(_activeSegmentSlot, expectedSlot))
        {
            return null;
        }

        _activeSegmentSlot = null;
        return expectedSlot;
    }
}
