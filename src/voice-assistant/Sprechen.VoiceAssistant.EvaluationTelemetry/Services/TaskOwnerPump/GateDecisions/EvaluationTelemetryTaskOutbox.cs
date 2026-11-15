using Microsoft.Extensions.Logging;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Entscheidet unter der lokalen Sperre über den nächsten Versandschritt
/// und speichert dessen Ergebnis, bevor der asynchrone Aufgabenablauf fortfährt.
/// </summary>
internal sealed partial class EvaluationTelemetryTaskOutbox
{
    private bool TryTakeNextWorkUnderGate(
        EvaluationTelemetryTaskOwner owner,
        out EvaluationTelemetryStoredEvent? pendingEvent,
        out Task? checkpointSignal,
        out EvaluationTelemetryStoredEvent? appendedTerminalEvent,
        out EvaluationSessionAcceptance? acceptance,
        out bool cleanupCommitted,
        out Exception? completionFailure)
    {
        pendingEvent = null;
        checkpointSignal = null;
        appendedTerminalEvent = null;
        acceptance = null;
        cleanupCommitted = false;
        completionFailure = null;

        lock (_gate)
        {
            if (!_checkpoints.TryGetValue(owner.StorageKey, out var checkpoint))
            {
                return false;
            }

            if (!CanRestoreIdentity(checkpoint.Identity))
            {
                completionFailure = new InvalidOperationException("The operational lifecycle is no longer current.");
                return false;
            }

            if (checkpoint.Phase == EvaluationTelemetryOutboxPhases.Faulted)
            {
                completionFailure = new EvaluationTelemetryDeliveryRejectedException(
                    checkpoint.PermanentFailureStatusCode);
                return false;
            }

            if (checkpoint.Phase == EvaluationTelemetryOutboxPhases.Committed)
            {
                cleanupCommitted = true;
            }

            if (checkpoint.Phase == EvaluationTelemetryOutboxPhases.Accepted)
            {
                var receipt = checkpoint.AcceptedReceipt!;
                acceptance = new EvaluationSessionAcceptance(
                    checkpoint.SessionId,
                    receipt.EventId,
                    receipt.BodySha256);
            }

            if (checkpoint.PendingEvents.Count == 0
                && checkpoint.Phase == EvaluationTelemetryOutboxPhases.Ending)
            {
                (checkpoint, appendedTerminalEvent) = _eventFactory.AppendTerminalEvent(checkpoint);
                Persist(checkpoint);
            }

            if (cleanupCommitted)
            {
                // Die nachfolgende Bereinigung erhält einen eigenen Aufruf. Sie nimmt
                // die lokale Sperre erneut. Ihre Wiederholungswartezeit liegt außerhalb.
            }
            else if (checkpoint.PendingEvents.Count == 0)
            {
                checkpointSignal = GetCheckpointSignal(owner.StorageKey).Task;
            }
            else
            {
                pendingEvent = checkpoint.PendingEvents[0] with
                {
                    DeliveryAttempts = checked(checkpoint.PendingEvents[0].DeliveryAttempts + 1),
                };
                Persist(checkpoint with
                {
                    PendingEvents = [pendingEvent, .. checkpoint.PendingEvents.Skip(1)],
                });
            }
        }

        return true;
    }

    private bool TryApplyDeliveryOutcomeUnderGate(
        EvaluationTelemetryTaskOwner owner,
        EvaluationTelemetryStoredEvent pendingEvent,
        EvaluationTelemetryDeliveryResult result,
        out EvaluationTelemetryStoredEvent? terminalEvent,
        out Exception? completionFailure)
    {
        terminalEvent = null;
        completionFailure = null;

        lock (_gate)
        {
            var checkpoint = RequireOwnedHead(owner, pendingEvent);
            if (!CanRestoreIdentity(checkpoint.Identity))
            {
                completionFailure = new InvalidOperationException("The operational lifecycle is no longer current.");
                return false;
            }
            if (result.Status == EvaluationTelemetryDeliveryStatus.PermanentFailure)
            {
                checkpoint = checkpoint with
                {
                    Phase = EvaluationTelemetryOutboxPhases.Faulted,
                    PermanentFailureStatusCode = result.StatusCode,
                };
                Persist(checkpoint);
                completionFailure = new EvaluationTelemetryDeliveryRejectedException(result.StatusCode);
                _logger.LogError(
                    "Evaluation delivery permanently rejected {EventId} with status {StatusCode}; the run remains quarantined.",
                    pendingEvent.EventId,
                    result.StatusCode);
                return false;
            }

            if (string.Equals(pendingEvent.EventName, TelemetryEventNames.TaskEnd, StringComparison.Ordinal))
            {
                var receipt = new EvaluationTelemetryAcceptedReceipt(
                    pendingEvent.EventId,
                    pendingEvent.BodySha256,
                    pendingEvent.Body);
                checkpoint = checkpoint with
                {
                    Phase = EvaluationTelemetryOutboxPhases.Accepted,
                    PendingEvents = [.. checkpoint.PendingEvents.Skip(1)],
                    AcknowledgedEvents = [.. checkpoint.AcknowledgedEvents, pendingEvent],
                    AcceptedReceipt = receipt,
                };
                Persist(checkpoint);
                return true;
            }

            checkpoint = checkpoint with
            {
                PendingEvents = [.. checkpoint.PendingEvents.Skip(1)],
                AcknowledgedEvents = [.. checkpoint.AcknowledgedEvents, pendingEvent],
            };
            if (checkpoint.PendingEvents.Count == 0
                && checkpoint.Phase == EvaluationTelemetryOutboxPhases.Ending)
            {
                (checkpoint, terminalEvent) = _eventFactory.AppendTerminalEvent(checkpoint);
            }

            Persist(checkpoint);
        }

        return true;
    }
}
