using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Serialisiert Ereignisse einmalig und ergänzt ihre SHA-256-Prüfsumme für die Outbox. Das Abschlussereignis entsteht erst nach Bestätigung aller Vorgänger.
/// </summary>
internal sealed class EvaluationTelemetryStoredEventFactory(
    EvaluationTelemetryEnvelopeSerializer serializer,
    EvaluationSessionTelemetryPayloads payloads)
{
    private readonly EvaluationTelemetryEnvelopeSerializer _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    private readonly EvaluationSessionTelemetryPayloads _payloads = payloads ?? throw new ArgumentNullException(nameof(payloads));
    private readonly object _clockGate = new();
    private readonly Dictionary<string, long> _clientOffsetOrigins = new(StringComparer.Ordinal);

    internal EvaluationTelemetryOutboxCheckpoint CreateTaskCheckpoint(
        string sessionId,
        EvaluationTelemetryEnvelopeIdentity identity,
        object? taskStartPayload)
    {
        StartClientOffsetClock(sessionId);
        var storageKey = EvaluationTelemetryOutboxKeys.BuildTaskStorageKey(sessionId, identity);
        var taskStart = Create(identity, TelemetryEventNames.TaskStart, 1, taskStartPayload, sessionId: sessionId);
        return new EvaluationTelemetryOutboxCheckpoint(
            EvaluationTelemetryCheckpointValidator.SchemaVersion,
            storageKey,
            sessionId,
            IsTaskRun: true,
            identity,
            EvaluationTelemetryOutboxPhases.Active,
            TransitionAdoptionState: new EvaluationTelemetryTransitionAdoptionCheckpointState(
                LastStateVersion: 1,
                AdoptedTransitionIdentities: null,
                LedgerEntries:
                [
                    EvaluationTelemetryTransitionAdoptionCheckpointEntry.ForEventBatch(1, [taskStart.EventId]),
                ]),
            PendingEvents: [taskStart],
            AcknowledgedEvents: [],
            TerminalIntent: null,
            AcceptedReceipt: null,
            PermanentFailureStatusCode: null,
            ClientOffsetElapsedMs: ReadClientOffsetMs(sessionId));
    }

    internal (EvaluationTelemetryOutboxCheckpoint Checkpoint, EvaluationTelemetryStoredEvent TerminalEvent)
        AppendTerminalEvent(EvaluationTelemetryOutboxCheckpoint checkpoint)
    {
        if (checkpoint.Phase != EvaluationTelemetryOutboxPhases.Ending
            || checkpoint.TerminalIntent is null
            || checkpoint.PendingEvents.Count != 0)
        {
            throw new InvalidOperationException("The terminal evaluation event can only follow all acknowledged predecessors.");
        }

        var stateVersion = checkpoint.TransitionAdoptionState.LastStateVersion;
        var payload = EvaluationTelemetryTransitionAttacher.Attach(
            TelemetryEventNames.TaskEnd,
            _payloads.CreateTaskEnd(checkpoint.TerminalIntent)).Payload;
        var terminalEvent = Create(
            checkpoint.Identity,
            TelemetryEventNames.TaskEnd,
            stateVersion,
            payload,
            sessionId: checkpoint.SessionId);
        return (
            checkpoint with
            {
                PendingEvents = [terminalEvent],
                TransitionAdoptionState = checkpoint.TransitionAdoptionState with
                {
                    LedgerEntries =
                    [
                        .. checkpoint.TransitionAdoptionState.LedgerEntries,
                        EvaluationTelemetryTransitionAdoptionCheckpointEntry.ForEventBatch(
                            stateVersion,
                            [terminalEvent.EventId]),
                    ],
                },
            },
            terminalEvent);
    }

    internal EvaluationTelemetryStoredEvent Create(
        EvaluationTelemetryEnvelopeIdentity identity,
        string eventName,
        int stateVersion,
        object? payload,
        string? eventId = null,
        string? sessionId = null)
    {
        int? clientOffsetMs = string.IsNullOrEmpty(sessionId) ? null : ReadClientOffsetMs(sessionId);
        var serialized = _serializer.Serialize(
            eventId ?? Guid.NewGuid().ToString("N"),
            identity,
            eventName,
            stateVersion,
            payload,
            sessionId,
            clientOffsetMs);
        return new EvaluationTelemetryStoredEvent(
            serialized.EventId,
            serialized.EventName,
            serialized.StateVersion,
            serialized.Body,
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(serialized.Body))),
            DeliveryAttempts: 0);
    }

    internal void RestoreClientOffsetClockIfAbsent(string sessionId, long elapsedMs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        if (elapsedMs is < 0 or > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedMs));
        }

        lock (_clockGate)
        {
            if (_clientOffsetOrigins.ContainsKey(sessionId))
            {
                return;
            }

            _clientOffsetOrigins[sessionId] = Stopwatch.GetTimestamp()
                - elapsedMs * Stopwatch.Frequency / 1000;
        }
    }

    internal void ForgetClientOffsetClock(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        lock (_clockGate)
        {
            _clientOffsetOrigins.Remove(sessionId);
        }
    }

    internal int ReadClientOffsetMs(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        lock (_clockGate)
        {
            if (!_clientOffsetOrigins.TryGetValue(sessionId, out var origin))
            {
                throw new InvalidOperationException("The client offset clock has no origin for this session.");
            }

            var elapsed = (Stopwatch.GetTimestamp() - origin) * 1000 / Stopwatch.Frequency;
            if (elapsed is < 0 or > int.MaxValue)
            {
                throw new InvalidOperationException("The client offset is out of range.");
            }

            return (int)elapsed;
        }
    }

    private void StartClientOffsetClock(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        lock (_clockGate)
        {
            _clientOffsetOrigins[sessionId] = Stopwatch.GetTimestamp();
        }
    }
}
