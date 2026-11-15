using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Prüft die Ereignisfolge und den exakten Abschlussbeleg einer Aufgabe,
/// damit ein angenommener Abschluss nur auf bestätigten Vorgängern beruht.
/// </summary>
internal sealed partial class EvaluationTelemetryCheckpointValidator
{
    private void ValidateTask(
        EvaluationTelemetryOutboxCheckpoint checkpoint,
        IReadOnlyList<EvaluationTelemetryStoredEvent> events)
    {
        if (!string.Equals(events[0].EventName, TelemetryEventNames.TaskStart, StringComparison.Ordinal)
            || events.Count(storedEvent => string.Equals(storedEvent.EventName, TelemetryEventNames.TaskStart, StringComparison.Ordinal)) != 1)
        {
            throw new JsonException("An evaluation task must begin with exactly one task_start event.");
        }

        var terminalEvents = events
            .Where(storedEvent => string.Equals(storedEvent.EventName, TelemetryEventNames.TaskEnd, StringComparison.Ordinal))
            .ToArray();
        if (terminalEvents.Length > 1
            || (terminalEvents.Length == 1 && !ReferenceEquals(terminalEvents[0], events[^1])))
        {
            throw new JsonException("An evaluation task may contain one terminal event at the tail.");
        }

        if (checkpoint.Phase == EvaluationTelemetryOutboxPhases.Active && terminalEvents.Length != 0)
        {
            throw new JsonException("An active evaluation task cannot contain task_end.");
        }

        if (checkpoint.Phase == EvaluationTelemetryOutboxPhases.Ending
            && terminalEvents.Length == 0
            && checkpoint.PendingEvents.Count == 0)
        {
            throw new JsonException("An ending evaluation task must have a pending predecessor or task_end.");
        }

        if (checkpoint.Phase is EvaluationTelemetryOutboxPhases.Accepted or EvaluationTelemetryOutboxPhases.Committed)
        {
            ValidateAcceptedTask(checkpoint, terminalEvents);
        }
        else if (terminalEvents.Length == 1)
        {
            ValidatePendingTerminalTask(checkpoint, terminalEvents[0]);
        }
    }

    private void ValidateAcceptedTask(
        EvaluationTelemetryOutboxCheckpoint checkpoint,
        IReadOnlyList<EvaluationTelemetryStoredEvent> terminalEvents)
    {
        if (terminalEvents.Count != 1
            || checkpoint.PendingEvents.Count != 0
            || !ReferenceEquals(checkpoint.AcknowledgedEvents[^1], terminalEvents[0])
            || checkpoint.AcceptedReceipt is null
            || !string.Equals(checkpoint.AcceptedReceipt.EventId, terminalEvents[0].EventId, StringComparison.Ordinal)
            || !string.Equals(checkpoint.AcceptedReceipt.BodySha256, terminalEvents[0].BodySha256, StringComparison.Ordinal)
            || !string.Equals(checkpoint.AcceptedReceipt.Body, terminalEvents[0].Body, StringComparison.Ordinal))
        {
            throw new JsonException("An accepted evaluation task must retain the exact acknowledged terminal receipt.");
        }

        ValidateTerminalBody(checkpoint, terminalEvents[0]);
    }

    private void ValidatePendingTerminalTask(
        EvaluationTelemetryOutboxCheckpoint checkpoint,
        EvaluationTelemetryStoredEvent terminalEvent)
    {
        if (checkpoint.TerminalIntent is null
            || checkpoint.PendingEvents.Count != 1
            || !ReferenceEquals(checkpoint.PendingEvents[0], terminalEvent))
        {
            throw new JsonException("task_end can only follow acknowledged predecessor events.");
        }

        ValidateTerminalBody(checkpoint, terminalEvent);
    }

    private static void ValidateOutsideTask(
        EvaluationTelemetryOutboxCheckpoint checkpoint,
        IReadOnlyList<EvaluationTelemetryStoredEvent> events)
    {
        if (events.Count != 1
            || checkpoint.AcknowledgedEvents.Count != 0
            || !string.Equals(events[0].EventId, checkpoint.SessionId, StringComparison.Ordinal)
            || events[0].EventName is TelemetryEventNames.TaskStart or TelemetryEventNames.TaskEnd)
        {
            throw new JsonException("An outside-task checkpoint must contain one pending non-task event.");
        }
    }

    private void ValidateTerminalBody(
        EvaluationTelemetryOutboxCheckpoint checkpoint,
        EvaluationTelemetryStoredEvent terminalEvent)
    {
        var expectedPayload = EvaluationTelemetryTransitionAttacher.Attach(
            TelemetryEventNames.TaskEnd,
            _payloads.CreateTaskEnd(checkpoint.TerminalIntent!)).Payload;
        var expectedData = JsonSerializer.SerializeToElement(expectedPayload);
        using var document = JsonDocument.Parse(terminalEvent.Body);
        var actualData = document.RootElement.GetProperty(TelemetryEnvelopeFieldNames.Data);
        if (!JsonElement.DeepEquals(actualData, expectedData))
        {
            throw new JsonException("The terminal event does not match its durable terminal intent and acknowledged history.");
        }
    }
}
