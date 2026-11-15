using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Rechnet die gespeicherte Übergangsfolge erneut durch und vergleicht Ereignisübergänge und Zustandsversion mit dem gespeicherten Aufgabenstand.
/// </summary>
internal sealed partial class EvaluationTelemetryCheckpointValidator
{
    private static EvaluationTelemetryTransitionReplayState ReplayTransitionState(
        EvaluationTelemetryOutboxCheckpoint checkpoint,
        IReadOnlyList<EvaluationTelemetryStoredEvent> events,
        IReadOnlyList<EvaluationTelemetryCheckpointEventProjectionValidation> validatedEvents)
    {
        if (!checkpoint.IsTaskRun)
        {
            if (checkpoint.TransitionAdoptionState.LedgerEntries.Count != 0)
            {
                throw new JsonException("An outside-task checkpoint cannot contain a transition adoption ledger.");
            }

            ValidateOutsideTaskTransitions(events, validatedEvents);
            return new EvaluationTelemetryTransitionReplayState(
                TelemetryEnvelopeContextDefaults.OutsideTaskStateVersion,
                null);
        }

        ValidateEventPartition(checkpoint.TransitionAdoptionState.LedgerEntries, events);
        var eventIndexById = events
            .Select((storedEvent, index) => (storedEvent.EventId, Index: index))
            .ToDictionary(static item => item.EventId, static item => item.Index, StringComparer.Ordinal);
        var state = new EvaluationTelemetryTransitionReplayState(1, null);
        foreach (var entry in checkpoint.TransitionAdoptionState.LedgerEntries)
        {
            state = entry.Kind switch
            {
                EvaluationTelemetryTransitionAdoptionCheckpointEntry.EventBatchKind => ReplayEventBatch(
                    entry,
                    events,
                    validatedEvents,
                    eventIndexById,
                    state),
                EvaluationTelemetryTransitionAdoptionCheckpointEntry.CompletionBaselineKind => ReplayCompletionBaseline(
                    entry,
                    state),
                _ => throw new JsonException("An evaluation transition adoption entry has an unknown kind."),
            };
        }

        return state;
    }

    private static void ValidateEventPartition(
        IReadOnlyList<EvaluationTelemetryTransitionAdoptionCheckpointEntry> entries,
        IReadOnlyList<EvaluationTelemetryStoredEvent> events)
    {
        if (entries.Count == 0)
        {
            throw new JsonException("A task checkpoint must contain a transition adoption ledger.");
        }

        var eventIds = new List<string>(events.Count);
        foreach (var entry in entries)
        {
            if (entry.EventIds is null || entry.TransitionIdentities is null || entry.StateVersion < 0)
            {
                throw new JsonException("An evaluation transition adoption entry is incomplete.");
            }

            switch (entry.Kind)
            {
                case EvaluationTelemetryTransitionAdoptionCheckpointEntry.EventBatchKind:
                    if (entry.EventIds.Count == 0 || entry.TransitionIdentities.Count != 0)
                    {
                        throw new JsonException("An event-batch adoption entry violates its one-of contract.");
                    }

                    eventIds.AddRange(entry.EventIds);
                    break;
                case EvaluationTelemetryTransitionAdoptionCheckpointEntry.CompletionBaselineKind:
                    if (entry.EventIds.Count != 0 || entry.TransitionIdentities.Count == 0)
                    {
                        throw new JsonException("A completion-baseline adoption entry violates its one-of contract.");
                    }

                    break;
                default:
                    throw new JsonException("An evaluation transition adoption entry has an unknown kind.");
            }
        }

        if (!eventIds.SequenceEqual(events.Select(static storedEvent => storedEvent.EventId), StringComparer.Ordinal))
        {
            throw new JsonException("The transition adoption ledger does not exactly partition the checkpoint events.");
        }
    }

    private static EvaluationTelemetryTransitionReplayState ReplayEventBatch(
        EvaluationTelemetryTransitionAdoptionCheckpointEntry entry,
        IReadOnlyList<EvaluationTelemetryStoredEvent> events,
        IReadOnlyList<EvaluationTelemetryCheckpointEventProjectionValidation> validatedEvents,
        IReadOnlyDictionary<string, int> eventIndexById,
        EvaluationTelemetryTransitionReplayState state)
    {
        var eventIndexes = entry.EventIds.Select(eventId => eventIndexById[eventId]).ToArray();
        var projectedBatch = eventIndexes
            .SelectMany(index => validatedEvents[index].ProjectedTransitions)
            .ToArray();
        var adoption = EvaluationTelemetryTransitionAdoptionReducer.Advance(
            state.StateVersion,
            state.AdoptedTransitionIdentities,
            projectedBatch);
        if (entry.StateVersion != adoption.StateVersion)
        {
            throw new JsonException("An event-batch adoption entry has an invalid state version.");
        }

        var emittedIdentities = adoption.EmittedTransitions
            .Select(EvaluationTelemetryTransitionAdoptionReducer.BuildIdentity)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var index in eventIndexes)
        {
            var expectedRecorded = validatedEvents[index].ProjectedTransitions
                .Where(transition => emittedIdentities.Contains(
                    EvaluationTelemetryTransitionAdoptionReducer.BuildIdentity(transition)))
                .ToArray();
            if (events[index].StateVersion != adoption.StateVersion
                || !validatedEvents[index].RecordedTransitions.SequenceEqual(expectedRecorded))
            {
                throw new JsonException(
                    "An evaluation checkpoint event does not match its durable event-batch adoption.");
            }
        }

        return new EvaluationTelemetryTransitionReplayState(
            adoption.StateVersion,
            adoption.AdoptedTransitionIdentities);
    }

    private static EvaluationTelemetryTransitionReplayState ReplayCompletionBaseline(
        EvaluationTelemetryTransitionAdoptionCheckpointEntry entry,
        EvaluationTelemetryTransitionReplayState state)
    {
        EvaluationTelemetryTransitionAdoptionResult adoption;
        try
        {
            adoption = EvaluationTelemetryTransitionAdoptionReducer.PrimeCompletionBaselineIdentities(
                state.StateVersion,
                state.AdoptedTransitionIdentities,
                entry.TransitionIdentities);
        }
        catch (InvalidDataException exception)
        {
            throw new JsonException("A completion-baseline adoption entry is invalid.", exception);
        }

        if (entry.StateVersion != state.StateVersion
            || adoption.StateVersion != state.StateVersion
            || adoption.EmittedTransitions.Count != 0)
        {
            throw new JsonException("A completion baseline must not emit or advance state.");
        }

        return state with { AdoptedTransitionIdentities = adoption.AdoptedTransitionIdentities };
    }

    private static void ValidateOutsideTaskTransitions(
        IReadOnlyList<EvaluationTelemetryStoredEvent> events,
        IReadOnlyList<EvaluationTelemetryCheckpointEventProjectionValidation> validatedEvents)
    {
        for (var index = 0; index < events.Count; index++)
        {
            if (events[index].StateVersion != TelemetryEnvelopeContextDefaults.OutsideTaskStateVersion
                || !validatedEvents[index].RecordedTransitions.SequenceEqual(validatedEvents[index].ProjectedTransitions))
            {
                throw new JsonException("An outside-task event does not match its canonical transition projection.");
            }
        }
    }

    private static void ValidateReplayResult(
        EvaluationTelemetryOutboxCheckpoint checkpoint,
        EvaluationTelemetryTransitionReplayState replayedState)
    {
        if (checkpoint.TransitionAdoptionState.LastStateVersion != replayedState.StateVersion
            || !SequenceEqualNullable(
                checkpoint.TransitionAdoptionState.AdoptedTransitionIdentities,
                replayedState.AdoptedTransitionIdentities))
        {
            throw new JsonException("Evaluation checkpoint durable transition state does not match its adoption ledger.");
        }
    }

    private static bool SequenceEqualNullable(
        IReadOnlyList<string>? left,
        IReadOnlyList<string>? right)
        => left is null ? right is null : right is not null && left.SequenceEqual(right, StringComparer.Ordinal);
}
