using System.Text.Json;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Prüft gespeicherte Aufgabenstände einschließlich Identität, Phase und
/// nachgerechneter Übergangshistorie, bevor die Outbox sie wieder übernimmt.
/// </summary>
internal sealed partial class EvaluationTelemetryCheckpointValidator(
    EvaluationSessionTelemetryPayloads payloads,
    EvaluationTelemetryCheckpointEventValidator eventValidator,
    IOperationalProbeSession operationalProbeSession)
{
    internal const int SchemaVersion = 1;
    private static readonly IReadOnlySet<string> ParticipantTerminalClassifications =
        new HashSet<string>(StringComparer.Ordinal)
        {
            TelemetryTaskTerminationClassifications.Completed,
            TelemetryTaskTerminationClassifications.ParticipantCancelled,
            TelemetryTaskTerminationClassifications.SessionAbandoned,
            TelemetryTaskTerminationClassifications.TechnicalFailure,
        };
    private readonly EvaluationSessionTelemetryPayloads _payloads =
        payloads ?? throw new ArgumentNullException(nameof(payloads));
    private readonly EvaluationTelemetryCheckpointEventValidator _eventValidator =
        eventValidator ?? throw new ArgumentNullException(nameof(eventValidator));

    internal void ValidateSerializedCheckpoint(string serialized)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serialized);

        _eventValidator.ValidateSerializedCheckpoint(serialized);
        using var document = JsonDocument.Parse(serialized);
        if (!document.RootElement.TryGetProperty("client_offset_elapsed_ms", out var elapsed)
            || elapsed.ValueKind != JsonValueKind.Number
            || !elapsed.TryGetInt64(out var value)
            || value < 0
            || value > int.MaxValue)
        {
            throw new JsonException("Evaluation checkpoint client_offset_elapsed_ms is missing or out of range.");
        }
    }

    internal void Validate(string storageKey, EvaluationTelemetryOutboxCheckpoint checkpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentNullException.ThrowIfNull(checkpoint);

        ValidateStructure(checkpoint);
        ValidateProducedOriginClass(checkpoint);
        ValidatePhase(checkpoint);
        ValidateStorageIdentity(storageKey, checkpoint);

        var events = checkpoint.AcknowledgedEvents.Concat(checkpoint.PendingEvents).ToArray();
        var validatedEvents = ValidateEvents(events, checkpoint.Identity);
        var replayedState = ReplayTransitionState(checkpoint, events, validatedEvents);
        ValidateReplayResult(checkpoint, replayedState);

        if (checkpoint.IsTaskRun)
        {
            ValidateTask(checkpoint, events);
        }
        else
        {
            ValidateOutsideTask(checkpoint, events);
        }
    }

    private static void ValidateStructure(EvaluationTelemetryOutboxCheckpoint checkpoint)
    {
        if (checkpoint.SchemaVersion != SchemaVersion
            || string.IsNullOrWhiteSpace(checkpoint.StorageKey)
            || string.IsNullOrWhiteSpace(checkpoint.SessionId)
            || checkpoint.Identity is null
            || checkpoint.TransitionAdoptionState is null
            || checkpoint.TransitionAdoptionState.LedgerEntries is null
            || checkpoint.PendingEvents is null
            || checkpoint.AcknowledgedEvents is null
            || checkpoint.ClientOffsetElapsedMs < 0
            || checkpoint.ClientOffsetElapsedMs > int.MaxValue
            || (!checkpoint.IsTaskRun && checkpoint.ClientOffsetElapsedMs != 0)
            || !EvaluationTelemetryOutboxPhases.All.Contains(checkpoint.Phase))
        {
            throw new JsonException("Evaluation checkpoint structure is invalid.");
        }
    }

    private static void ValidateStorageIdentity(
        string storageKey,
        EvaluationTelemetryOutboxCheckpoint checkpoint)
    {
        var expectedStorageKey = checkpoint.IsTaskRun
            ? EvaluationTelemetryOutboxKeys.BuildTaskStorageKey(checkpoint.SessionId, checkpoint.Identity)
            : EvaluationTelemetryOutboxKeys.BuildOutsideTaskStorageKey(checkpoint.SessionId);
        if (!string.Equals(storageKey, checkpoint.StorageKey, StringComparison.Ordinal)
            || !string.Equals(storageKey, expectedStorageKey, StringComparison.Ordinal))
        {
            throw new JsonException("Evaluation checkpoint storage identity is invalid.");
        }
    }

    private EvaluationTelemetryCheckpointEventProjectionValidation[] ValidateEvents(
        IReadOnlyList<EvaluationTelemetryStoredEvent> events,
        EvaluationTelemetryEnvelopeIdentity identity)
    {
        if (events.Count == 0)
        {
            throw new JsonException("Evaluation checkpoint must contain at least one event.");
        }

        var validatedEvents = events
            .Select(storedEvent => _eventValidator.Validate(storedEvent, identity))
            .ToArray();
        var eventIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var storedEvent in events)
        {
            if (!eventIds.Add(storedEvent.EventId))
            {
                throw new JsonException("Evaluation checkpoint event identifiers must be unique.");
            }
        }

        return validatedEvents;
    }

    // Bei Prüfläufen entscheidet die gebundene Sitzung anhand des Speicherschlüssels, ob der gespeicherte Aufgabenstand zu ihr gehört.
    private void ValidateProducedOriginClass(EvaluationTelemetryOutboxCheckpoint checkpoint)
    {
        if (checkpoint.Identity.RecordProvenance == TelemetryRecordProvenances.OperationalProbe
            && !operationalProbeSession.OwnsTaskStorageKey(checkpoint.StorageKey))
        {
            throw new JsonException("Evaluation checkpoint operational identity is not currently bound.");
        }
    }

    private static void ValidatePhase(EvaluationTelemetryOutboxCheckpoint checkpoint)
    {
        ValidateTerminalClassification(checkpoint);

        switch (checkpoint.Phase)
        {
            case EvaluationTelemetryOutboxPhases.Active:
                ValidateActivePhase(checkpoint);
                break;
            case EvaluationTelemetryOutboxPhases.Ending:
                ValidateEndingPhase(checkpoint);
                break;
            case EvaluationTelemetryOutboxPhases.Accepted:
            case EvaluationTelemetryOutboxPhases.Committed:
                ValidateAcceptedPhase(checkpoint);
                break;
            case EvaluationTelemetryOutboxPhases.OutsideTask:
                ValidateOutsideTaskPhase(checkpoint);
                break;
            case EvaluationTelemetryOutboxPhases.Faulted:
                ValidateFaultedPhase(checkpoint);
                break;
        }
    }

    private static void ValidateTerminalClassification(EvaluationTelemetryOutboxCheckpoint checkpoint)
    {
        if (checkpoint.TerminalIntent is not null
            && !ParticipantTerminalClassifications.Contains(
                checkpoint.TerminalIntent.TerminationClassification))
        {
            throw new JsonException("An evaluation checkpoint has an unknown terminal classification.");
        }
    }

    private static void ValidateActivePhase(EvaluationTelemetryOutboxCheckpoint checkpoint)
    {
        if (!checkpoint.IsTaskRun
            || checkpoint.TerminalIntent is not null
            || checkpoint.AcceptedReceipt is not null
            || checkpoint.PermanentFailureStatusCode is not null)
        {
            throw new JsonException("An active evaluation checkpoint has invalid terminal state.");
        }
    }

    private static void ValidateEndingPhase(EvaluationTelemetryOutboxCheckpoint checkpoint)
    {
        if (!checkpoint.IsTaskRun
            || checkpoint.TerminalIntent is null
            || checkpoint.AcceptedReceipt is not null
            || checkpoint.PermanentFailureStatusCode is not null)
        {
            throw new JsonException("An ending evaluation checkpoint has invalid terminal state.");
        }
    }

    private static void ValidateAcceptedPhase(EvaluationTelemetryOutboxCheckpoint checkpoint)
    {
        if (!checkpoint.IsTaskRun
            || checkpoint.TerminalIntent is null
            || checkpoint.AcceptedReceipt is null
            || checkpoint.PermanentFailureStatusCode is not null)
        {
            throw new JsonException("An accepted or committed evaluation checkpoint has invalid terminal state.");
        }
    }

    private static void ValidateOutsideTaskPhase(EvaluationTelemetryOutboxCheckpoint checkpoint)
    {
        if (checkpoint.IsTaskRun
            || checkpoint.TerminalIntent is not null
            || checkpoint.AcceptedReceipt is not null
            || checkpoint.PermanentFailureStatusCode is not null)
        {
            throw new JsonException("An outside-task evaluation checkpoint has invalid state.");
        }
    }

    private static void ValidateFaultedPhase(EvaluationTelemetryOutboxCheckpoint checkpoint)
    {
        if (checkpoint.PermanentFailureStatusCode is null || checkpoint.AcceptedReceipt is not null)
        {
            throw new JsonException("A faulted evaluation checkpoint needs its rejection status.");
        }
    }

}
