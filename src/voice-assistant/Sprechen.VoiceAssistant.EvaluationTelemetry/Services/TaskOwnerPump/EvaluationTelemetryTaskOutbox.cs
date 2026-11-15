using Microsoft.Extensions.Logging;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Versendet die Ereignisse eines exklusiv gesperrten Durchlaufs bis zur Bestätigung
/// und lokalen Bereinigung. Warte- und Netzwerkaufrufe halten die interne Zustandssperre nicht.
/// </summary>
internal sealed partial class EvaluationTelemetryTaskOutbox
{
    private void StartTaskOwner(EvaluationTelemetryTaskOwner owner)
        => _ = RunTaskOwnerAsync(owner);

    private async Task RunTaskOwnerAsync(EvaluationTelemetryTaskOwner owner)
    {
        Exception? completionFailure = null;
        try
        {
            while (!_shutdown.IsCancellationRequested)
            {
                if (!TryTakeNextWorkUnderGate(
                        owner,
                        out var pendingEvent,
                        out var checkpointSignal,
                        out var appendedTerminalEvent,
                        out var acceptance,
                        out var cleanupCommitted,
                        out var takeFailure))
                {
                    completionFailure = takeFailure;
                    return;
                }

                if (appendedTerminalEvent is not null)
                {
                    LogEnqueued(appendedTerminalEvent);
                }

                if (acceptance is not null)
                {
                    owner.TerminalAcceptance.TrySetResult(acceptance);
                }

                if (cleanupCommitted)
                {
                    if (await TryClearCommittedAsync(owner).ConfigureAwait(false))
                    {
                        return;
                    }

                    continue;
                }

                if (checkpointSignal is not null)
                {
                    await checkpointSignal.WaitAsync(_shutdown.Token).ConfigureAwait(false);
                    continue;
                }

                var result = await DeliverAsync(pendingEvent!).ConfigureAwait(false);
                if (result.Status == EvaluationTelemetryDeliveryStatus.TransientFailure)
                {
                    await _retryDelay.WaitAsync(
                        pendingEvent!.DeliveryAttempts,
                        result.RetryAfter,
                        _shutdown.Token).ConfigureAwait(false);
                    continue;
                }

                if (!TryApplyDeliveryOutcomeUnderGate(
                        owner,
                        pendingEvent!,
                        result,
                        out var terminalEvent,
                        out var deliveryFailure))
                {
                    completionFailure = deliveryFailure;
                    return;
                }

                if (terminalEvent is not null)
                {
                    LogEnqueued(terminalEvent);
                }
            }
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            completionFailure = exception;
            _logger.LogError(
                exception,
                "Evaluation identity owner {LeaseKey} stopped; its persisted events remain quarantined.",
                owner.LeaseKey);
        }
        finally
        {
            await CompleteTaskOwnerAsync(owner, completionFailure).ConfigureAwait(false);
        }
    }

    private async Task CompleteTaskOwnerAsync(
        EvaluationTelemetryTaskOwner owner,
        Exception? completionFailure)
    {
        try
        {
            await owner.Lease.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            completionFailure ??= exception;
        }

        lock (_gate)
        {
            if (_taskOwners.TryGetValue(owner.LeaseKey, out var currentOwner)
                && ReferenceEquals(currentOwner, owner))
            {
                _taskOwners.Remove(owner.LeaseKey);
            }
        }

        if (completionFailure is not null)
        {
            owner.TerminalAcceptance.TrySetException(completionFailure);
            owner.LocalCommit.TrySetException(completionFailure);
        }
    }

    private async Task<bool> TryClearCommittedAsync(EvaluationTelemetryTaskOwner owner)
    {
        try
        {
            lock (_gate)
            {
                _store.Delete(owner.StorageKey);
                _checkpoints.Remove(owner.StorageKey);
                _checkpointSignals.Remove(owner.StorageKey);
                _eventFactory.ForgetClientOffsetClock(owner.SessionId);
            }

            owner.LocalCommit.TrySetResult();
            return true;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            _logger.LogWarning(
                exception,
                "Committed evaluation checkpoint {StorageKey} could not be cleared; cleanup will retry under the identity lease.",
                owner.StorageKey);
            await _retryDelay.WaitAsync(1, retryAfter: null, cancellationToken: _shutdown.Token).ConfigureAwait(false);
            return false;
        }
    }


    private async Task<EvaluationTelemetryDeliveryResult> DeliverAsync(EvaluationTelemetryStoredEvent pendingEvent)
    {
        try
        {
            return await _transport.DeliverAsync(pendingEvent.Body, _shutdown.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Evaluation delivery attempt for {EventId} failed before an acknowledgement.",
                pendingEvent.EventId);
            return new EvaluationTelemetryDeliveryResult(EvaluationTelemetryDeliveryStatus.TransientFailure);
        }
    }
}
