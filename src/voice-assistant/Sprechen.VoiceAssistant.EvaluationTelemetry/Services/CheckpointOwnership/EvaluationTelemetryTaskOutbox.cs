using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Verwaltet gespeicherte Aufgabenereignisse, ihre exklusive Verarbeitung und den Versand unveränderter Ereignishüllen.
/// </summary>
internal sealed partial class EvaluationTelemetryTaskOutbox
{
    // Das Zustellungsprotokoll übernimmt seine Platzhalter aus der gemeinsamen Hüllfeldliste. Die Positionsargumente in LogEnqueued müssen dazu passen.
    private const string DeliveryEnqueuedMessageTemplate =
        "evaluation_delivery_enqueued " + EvaluationTelemetryLogTemplate.EnvelopeMemberPlaceholders;

    private EvaluationTelemetryTaskOwner RegisterTaskOwner(
        string leaseKey,
        EvaluationTelemetryOutboxCheckpoint checkpoint,
        IEvaluationTelemetryExclusiveLeaseHandle lease,
        bool restoredTerminalAdoptionAvailable)
    {
        var owner = new EvaluationTelemetryTaskOwner(
            leaseKey,
            checkpoint.StorageKey,
            checkpoint.SessionId,
            lease,
            restoredTerminalAdoptionAvailable);
        if (checkpoint.Phase is EvaluationTelemetryOutboxPhases.Accepted or EvaluationTelemetryOutboxPhases.Committed)
        {
            var receipt = checkpoint.AcceptedReceipt!;
            owner.TerminalAcceptance.TrySetResult(new EvaluationSessionAcceptance(
                checkpoint.SessionId,
                receipt.EventId,
                receipt.BodySha256));
        }

        lock (_gate)
        {
            if (_taskOwners.ContainsKey(leaseKey))
            {
                throw new InvalidOperationException("The evaluation identity already has a process-local owner.");
            }

            _checkpoints[checkpoint.StorageKey] = checkpoint;
            _taskOwners.Add(leaseKey, owner);
        }

        return owner;
    }

    private EvaluationTelemetryTaskOwner? FindTaskOwner(string sessionId)
    {
        return _taskOwners.Values.SingleOrDefault(
            owner => string.Equals(owner.SessionId, sessionId, StringComparison.Ordinal));
    }

    private EvaluationTelemetryOutboxCheckpoint? FindTaskCheckpoint(string sessionId)
    {
        return _checkpoints.Values.SingleOrDefault(
            checkpoint => checkpoint.IsTaskRun
                && string.Equals(checkpoint.SessionId, sessionId, StringComparison.Ordinal));
    }

    private EvaluationTelemetryOutboxCheckpoint RequireOwnedHead(
        EvaluationTelemetryTaskOwner owner,
        EvaluationTelemetryStoredEvent pendingEvent)
    {
        if (!_checkpoints.TryGetValue(owner.StorageKey, out var checkpoint)
            || checkpoint.PendingEvents.Count == 0
            || !string.Equals(checkpoint.PendingEvents[0].EventId, pendingEvent.EventId, StringComparison.Ordinal)
            || !string.Equals(checkpoint.PendingEvents[0].Body, pendingEvent.Body, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Evaluation outbox FIFO ownership was lost while delivering an event.");
        }

        return checkpoint;
    }

    private EvaluationTelemetryOutboxCheckpoint? LoadIdentityCheckpoint(
        EvaluationTelemetryEnvelopeIdentity identity)
    {
        var matchingKeys = _store.ListKeys()
            .Where(static key => key.StartsWith(EvaluationTelemetryOutboxKeys.StorageKeyPrefix, StringComparison.Ordinal))
            .Where(key => EvaluationTelemetryOutboxKeys.TryReadTaskIdentity(key, out var keyIdentity, out _)
                && keyIdentity == identity)
            .ToArray();
        if (matchingKeys.Length > 1)
        {
            throw new JsonException("More than one evaluation checkpoint exists for the same task identity.");
        }

        if (matchingKeys.Length == 0)
        {
            return null;
        }

        var serialized = _store.Read(matchingKeys[0]);
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return null;
        }

        var checkpoint = DeserializeCheckpoint(matchingKeys[0], serialized);
        lock (_gate)
        {
            _checkpoints[checkpoint.StorageKey] = checkpoint;
        }

        return checkpoint;
    }

    private InvalidDataException QuarantineIdentity(string leaseKey, Exception cause)
    {
        lock (_gate)
        {
            if (_identityQuarantineFailures.TryGetValue(leaseKey, out var existing))
            {
                return existing;
            }

            var failure = new InvalidDataException(
                "The durable evaluation checkpoint for this task identity is quarantined.",
                cause);
            _identityQuarantineFailures.Add(leaseKey, failure);
            return failure;
        }
    }

    private void ThrowIfIdentityQuarantined(string leaseKey)
    {
        if (_identityQuarantineFailures.TryGetValue(leaseKey, out var failure))
        {
            throw failure;
        }
    }

    private EvaluationTelemetryOutboxCheckpoint? ReloadCheckpoint(string storageKey)
    {
        var serialized = _store.Read(storageKey);
        if (string.IsNullOrWhiteSpace(serialized))
        {
            lock (_gate)
            {
                _checkpoints.Remove(storageKey);
            }

            return null;
        }

        var checkpoint = DeserializeCheckpoint(storageKey, serialized);
        lock (_gate)
        {
            _checkpoints[storageKey] = checkpoint;
        }

        return checkpoint;
    }

    private EvaluationTelemetryOutboxCheckpoint DeserializeCheckpoint(string storageKey, string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            throw new JsonException("Evaluation checkpoint body is missing.");
        }

        _validator.ValidateSerializedCheckpoint(serialized);
        var checkpoint = JsonSerializer.Deserialize<EvaluationTelemetryOutboxCheckpoint>(
            serialized,
            CheckpointSerializerOptions)
            ?? throw new JsonException("Evaluation checkpoint body is null.");
        _validator.Validate(storageKey, checkpoint);
        if (checkpoint.IsTaskRun)
        {
            _eventFactory.RestoreClientOffsetClockIfAbsent(
                checkpoint.SessionId,
                checkpoint.ClientOffsetElapsedMs);
        }

        return checkpoint;
    }

    private void Persist(EvaluationTelemetryOutboxCheckpoint checkpoint)
    {
        if (checkpoint.IsTaskRun)
        {
            checkpoint = checkpoint with
            {
                ClientOffsetElapsedMs = _eventFactory.ReadClientOffsetMs(checkpoint.SessionId),
            };
        }

        _validator.Validate(checkpoint.StorageKey, checkpoint);
        var serialized = JsonSerializer.Serialize(checkpoint, CheckpointSerializerOptions);
        _store.Write(checkpoint.StorageKey, serialized);
        _checkpoints[checkpoint.StorageKey] = checkpoint;
    }

    private TaskCompletionSource GetCheckpointSignal(string storageKey)
    {
        if (!_checkpointSignals.TryGetValue(storageKey, out var signal) || signal.Task.IsCompleted)
        {
            signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _checkpointSignals[storageKey] = signal;
        }

        return signal;
    }

    private void SignalCheckpoint(string storageKey)
    {
        TaskCompletionSource? signal;
        lock (_gate)
        {
            _checkpointSignals.Remove(storageKey, out signal);
        }

        signal?.TrySetResult();
    }

    private void LogEnqueued(EvaluationTelemetryStoredEvent storedEvent)
    {
        using var document = JsonDocument.Parse(storedEvent.Body);
        var root = document.RootElement;
        _logger.LogInformation(
            DeliveryEnqueuedMessageTemplate,
            root.GetProperty(TelemetryEnvelopeFieldNames.EventId).GetString(),
            root.TryGetProperty(TelemetryEnvelopeFieldNames.SessionId, out var sessionId)
                ? sessionId.GetString()
                : string.Empty,
            root.TryGetProperty(TelemetryEnvelopeFieldNames.ClientOffsetMs, out var clientOffset)
                && clientOffset.ValueKind == JsonValueKind.Number
                ? clientOffset.GetInt32().ToString()
                : string.Empty,
            root.GetProperty(TelemetryEnvelopeFieldNames.Event).GetString(),
            root.GetProperty(TelemetryEnvelopeFieldNames.ParticipantId).GetString(),
            root.GetProperty(TelemetryEnvelopeFieldNames.Condition).GetString(),
            root.GetProperty(TelemetryEnvelopeFieldNames.ApplicationId).GetString(),
            root.GetProperty(TelemetryEnvelopeFieldNames.TaskId).GetString(),
            root.GetProperty(TelemetryEnvelopeFieldNames.RecordProvenance).GetString(),
            root.GetProperty(TelemetryEnvelopeFieldNames.StateVersion).GetInt32(),
            root.GetProperty(TelemetryEnvelopeFieldNames.Data).GetRawText());
    }

    private void EnsureSessionBoundary(
        EvaluationTelemetryOutboxCheckpoint checkpoint,
        EvaluationTelemetryEnvelopeIdentity identity)
    {
        if (checkpoint.Identity != identity || !CanRestoreIdentity(identity))
        {
            throw new InvalidOperationException("Evaluation identity changed inside one outbox run.");
        }
    }

    private EvaluationTelemetryTaskOwner[] SnapshotTaskOwners()
    {
        lock (_gate)
        {
            return [.. _taskOwners.Values];
        }
    }
}
