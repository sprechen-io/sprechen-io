using Microsoft.Extensions.Logging;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Speichert und versendet Ereignisse außerhalb einer Formularaufgabe mit einer eigenen Sperre je Ereignis. Ihre Verarbeitung ist von laufenden Aufgaben getrennt.
/// </summary>
internal sealed partial class EvaluationTelemetryTaskOutbox
{
internal void EnqueueOutsideTask(
        EvaluationTelemetryEnvelopeIdentity identity,
        string eventName,
        object? payload)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        if (_operationalProbeSession.IsRequested || !ShouldCaptureCurrentRun(identity))
        {
            return;
        }

        var eventId = Guid.NewGuid().ToString("N");
        var storageKey = EvaluationTelemetryOutboxKeys.BuildOutsideTaskStorageKey(eventId);
        var storedEvent = _eventFactory.Create(
            identity,
            eventName,
            TelemetryEnvelopeContextDefaults.OutsideTaskStateVersion,
            payload,
            eventId);
        var checkpoint = new EvaluationTelemetryOutboxCheckpoint(
            EvaluationTelemetryCheckpointValidator.SchemaVersion,
            storageKey,
            eventId,
            IsTaskRun: false,
            identity,
            EvaluationTelemetryOutboxPhases.OutsideTask,
            TransitionAdoptionState: new EvaluationTelemetryTransitionAdoptionCheckpointState(
                TelemetryEnvelopeContextDefaults.OutsideTaskStateVersion,
                AdoptedTransitionIdentities: null,
                LedgerEntries: []),
            PendingEvents: [storedEvent],
            AcknowledgedEvents: [],
            TerminalIntent: null,
            AcceptedReceipt: null,
            PermanentFailureStatusCode: null,
            ClientOffsetElapsedMs: 0);

        lock (_gate)
        {
            Persist(checkpoint);
        }

        LogEnqueued(storedEvent);
        _ = EnsureInitializedAsync();
        StartOutsideTaskPump(storageKey);
    }

private void StartOutsideTaskPump(string storageKey)
    {
        lock (_gate)
        {
            if (!_activeOutsideTaskPumps.Add(storageKey))
            {
                return;
            }
        }

        _ = PumpOutsideTaskAsync(storageKey);
    }

    private async Task PumpOutsideTaskAsync(string storageKey)
    {
        try
        {
            var leaseKey = EvaluationTelemetryOutboxKeys.BuildOutsideTaskLeaseKey(storageKey);
            await using var lease = await _exclusiveLease.TryAcquireAsync(leaseKey, _shutdown.Token).ConfigureAwait(false);
            if (lease is null)
            {
                return;
            }

            if (_operationalProbeSession.IsRequested || !CanRestoreOutsideTask(storageKey))
            {
                return;
            }

            var checkpoint = ReloadCheckpoint(storageKey);
            if (checkpoint is null || checkpoint.Phase == EvaluationTelemetryOutboxPhases.Faulted)
            {
                return;
            }

            while (!_shutdown.IsCancellationRequested)
            {
                if (_operationalProbeSession.IsRequested || !CanRestoreIdentity(checkpoint.Identity))
                {
                    return;
                }

                EvaluationTelemetryStoredEvent pendingEvent;
                lock (_gate)
                {
                    checkpoint = _checkpoints[storageKey];
                    pendingEvent = checkpoint.PendingEvents[0] with
                    {
                        DeliveryAttempts = checked(checkpoint.PendingEvents[0].DeliveryAttempts + 1),
                    };
                    checkpoint = checkpoint with { PendingEvents = [pendingEvent] };
                    Persist(checkpoint);
                }

                var result = await DeliverAsync(pendingEvent).ConfigureAwait(false);
                if (result.Status == EvaluationTelemetryDeliveryStatus.TransientFailure)
                {
                    await _retryDelay.WaitAsync(
                        pendingEvent.DeliveryAttempts,
                        result.RetryAfter,
                        _shutdown.Token).ConfigureAwait(false);
                    continue;
                }

                lock (_gate)
                {
                    checkpoint = _checkpoints[storageKey];
                    if (result.Status == EvaluationTelemetryDeliveryStatus.PermanentFailure)
                    {
                        Persist(checkpoint with
                        {
                            Phase = EvaluationTelemetryOutboxPhases.Faulted,
                            PermanentFailureStatusCode = result.StatusCode,
                        });
                    }
                    else
                    {
                        _store.Delete(storageKey);
                        _checkpoints.Remove(storageKey);
                    }
                }

                return;
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Evaluation outside-task owner {StorageKey} stopped; its persisted event remains quarantined.",
                storageKey);
        }
        finally
        {
            lock (_gate)
            {
                _activeOutsideTaskPumps.Remove(storageKey);
            }
        }
    }
}
