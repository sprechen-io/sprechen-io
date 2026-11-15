using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Übernimmt zusammengehörige Beobachtungen in den gespeicherten Aufgabenstand und hält
/// ihre Übergänge samt Reihenfolge für die spätere Wiederanlaufprüfung fest.
/// </summary>
internal sealed partial class EvaluationTelemetryTaskOutbox
{
    internal void EnqueueTaskEvent(
        EvaluationSession session,
        EvaluationTelemetryEnvelopeIdentity identity,
        string eventName,
        object? payload,
        IReadOnlyList<TelemetryCanonicalTransition> projectedTransitions)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(projectedTransitions);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        EnqueueTaskEvents(
            session,
            identity,
            [new EvaluationTelemetryTaskEventInput(
                eventName,
                payload,
                projectedTransitions)]);
    }

    internal void EnqueueTaskEvents(
        EvaluationSession session,
        EvaluationTelemetryEnvelopeIdentity identity,
        IReadOnlyList<EvaluationTelemetryTaskEventInput> inputs)
    {
        ValidateTaskEventInputs(session, identity, inputs);
        if (inputs.Count == 0)
        {
            return;
        }

        IReadOnlyList<EvaluationTelemetryStoredEvent> storedEvents;
        string storageKey;
        lock (_gate)
        {
            if (_suppressedRuns.Contains(session.SessionId))
            {
                return;
            }

            var checkpoint = GetActiveOwnedCheckpoint(session, identity, out storageKey);
            (checkpoint, storedEvents) = AppendTaskEventBatch(checkpoint, identity, inputs);
            Persist(checkpoint);
        }

        foreach (var storedEvent in storedEvents)
        {
            LogEnqueued(storedEvent);
        }

        SignalCheckpoint(storageKey);
    }

    internal void PrimeTaskCompletionAdoption(
        EvaluationSession session,
        EvaluationTelemetryEnvelopeIdentity identity,
        IReadOnlyList<TelemetryCanonicalTransition> completionTransitions)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(completionTransitions);
        if (completionTransitions.Count == 0)
        {
            return;
        }

        lock (_gate)
        {
            if (_suppressedRuns.Contains(session.SessionId))
            {
                return;
            }

            var checkpoint = GetActiveOwnedCheckpoint(session, identity, out _);
            var adoption = EvaluationTelemetryTransitionAdoptionReducer.PrimeCompletionBaseline(
                checkpoint.TransitionAdoptionState.LastStateVersion,
                checkpoint.TransitionAdoptionState.AdoptedTransitionIdentities,
                completionTransitions);
            var baselineIdentities = completionTransitions
                .Select(EvaluationTelemetryTransitionAdoptionReducer.BuildIdentity)
                .ToArray();
            Persist(checkpoint with
            {
                TransitionAdoptionState = checkpoint.TransitionAdoptionState with
                {
                    LastStateVersion = adoption.StateVersion,
                    AdoptedTransitionIdentities = adoption.AdoptedTransitionIdentities,
                    LedgerEntries =
                    [
                        .. checkpoint.TransitionAdoptionState.LedgerEntries,
                        EvaluationTelemetryTransitionAdoptionCheckpointEntry.ForCompletionBaseline(
                            adoption.StateVersion,
                            baselineIdentities),
                    ],
                },
            });
        }
    }

    private static void ValidateTaskEventInputs(
        EvaluationSession session,
        EvaluationTelemetryEnvelopeIdentity identity,
        IReadOnlyList<EvaluationTelemetryTaskEventInput> inputs)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(inputs);
        foreach (var input in inputs)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(input.EventName);
            ArgumentNullException.ThrowIfNull(input.ProjectedTransitions);
        }
    }

    private EvaluationTelemetryOutboxCheckpoint GetActiveOwnedCheckpoint(
        EvaluationSession session,
        EvaluationTelemetryEnvelopeIdentity identity,
        out string storageKey)
    {
        var owner = FindTaskOwner(session.SessionId);
        if (owner is null)
        {
            ThrowUnownedTaskFailure(session.SessionId);
        }

        storageKey = owner!.StorageKey;
        var checkpoint = _checkpoints[storageKey];
        EnsureSessionBoundary(checkpoint, identity);
        if (checkpoint.Phase == EvaluationTelemetryOutboxPhases.Faulted)
        {
            throw new EvaluationTelemetryDeliveryRejectedException(checkpoint.PermanentFailureStatusCode);
        }

        if (checkpoint.Phase != EvaluationTelemetryOutboxPhases.Active)
        {
            throw new InvalidOperationException("No evaluation event can be appended after task completion was requested.");
        }

        return checkpoint;
    }

    private void ThrowUnownedTaskFailure(string sessionId)
    {
        var unownedCheckpoint = FindTaskCheckpoint(sessionId);
        if (unownedCheckpoint?.Phase == EvaluationTelemetryOutboxPhases.Faulted)
        {
            throw new EvaluationTelemetryDeliveryRejectedException(unownedCheckpoint.PermanentFailureStatusCode);
        }

        throw new InvalidOperationException("The evaluation task does not own its identity lease.");
    }

    private (EvaluationTelemetryOutboxCheckpoint Checkpoint, IReadOnlyList<EvaluationTelemetryStoredEvent> Events)
        AppendTaskEventBatch(
            EvaluationTelemetryOutboxCheckpoint checkpoint,
            EvaluationTelemetryEnvelopeIdentity identity,
            IReadOnlyList<EvaluationTelemetryTaskEventInput> inputs)
    {
        var projectedTransitions = inputs
            .SelectMany(static input => input.ProjectedTransitions)
            .ToArray();
        var adoption = EvaluationTelemetryTransitionAdoptionReducer.Advance(
            checkpoint.TransitionAdoptionState.LastStateVersion,
            checkpoint.TransitionAdoptionState.AdoptedTransitionIdentities,
            projectedTransitions);
        var emittedIdentities = adoption.EmittedTransitions
            .Select(EvaluationTelemetryTransitionAdoptionReducer.BuildIdentity)
            .ToHashSet(StringComparer.Ordinal);
        var storedEvents = inputs
            .Select(input => CreateStoredTaskEvent(
                input,
                identity,
                adoption,
                emittedIdentities,
                checkpoint.SessionId))
            .ToArray();
        var updatedCheckpoint = checkpoint with
        {
            TransitionAdoptionState = checkpoint.TransitionAdoptionState with
            {
                LastStateVersion = adoption.StateVersion,
                AdoptedTransitionIdentities = adoption.AdoptedTransitionIdentities,
                LedgerEntries =
                [
                    .. checkpoint.TransitionAdoptionState.LedgerEntries,
                    EvaluationTelemetryTransitionAdoptionCheckpointEntry.ForEventBatch(
                        adoption.StateVersion,
                        storedEvents.Select(static storedEvent => storedEvent.EventId).ToArray()),
                ],
            },
            PendingEvents = [.. checkpoint.PendingEvents, .. storedEvents],
        };
        return (updatedCheckpoint, storedEvents);
    }

    private EvaluationTelemetryStoredEvent CreateStoredTaskEvent(
        EvaluationTelemetryTaskEventInput input,
        EvaluationTelemetryEnvelopeIdentity identity,
        EvaluationTelemetryTransitionAdoptionResult adoption,
        IReadOnlySet<string> emittedIdentities,
        string sessionId)
    {
        var emittedTransitions = input.ProjectedTransitions
            .Where(transition => emittedIdentities.Contains(
                EvaluationTelemetryTransitionAdoptionReducer.BuildIdentity(transition)))
            .ToArray();
        var emittedPayload = EvaluationTelemetryTransitionAttacher.ReplaceTransitions(
            input.Payload,
            emittedTransitions);
        return _eventFactory.Create(
            identity,
            input.EventName,
            adoption.StateVersion,
            emittedPayload,
            sessionId: sessionId);
    }
}
