using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Bindet Aufgabenbeginn, Abschlusswunsch und lokalen Abschluss an dieselbe
/// Identitätssperre und den zugehörigen dauerhaften Empfangsbeleg.
/// </summary>
internal sealed partial class EvaluationTelemetryTaskOutbox
{
    internal async Task<EvaluationSession> StartTaskAsync(
        EvaluationSession proposedSession,
        EvaluationTelemetryEnvelopeIdentity identity,
        object? taskStartPayload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(proposedSession);
        ArgumentNullException.ThrowIfNull(identity);

        if (!ShouldCaptureCurrentRun(identity))
        {
            lock (_gate)
            {
                _suppressedRuns.Add(proposedSession.SessionId);
            }

            return proposedSession;
        }

        await EnsureInitializedAsync().WaitAsync(cancellationToken).ConfigureAwait(false);

        if (!ShouldCaptureCurrentRun(identity))
        {
            throw new InvalidOperationException("The task capture context changed during initialization.");
        }

        var leaseKey = EvaluationTelemetryOutboxKeys.BuildIdentityLeaseKey(identity);
        lock (_gate)
        {
            ThrowIfIdentityQuarantined(leaseKey);
            if (_taskOwners.TryGetValue(leaseKey, out var existingOwner))
            {
                var existingCheckpoint = _checkpoints[existingOwner.StorageKey];
                EnsureSessionBoundary(existingCheckpoint, identity);
                if (existingCheckpoint.Phase is EvaluationTelemetryOutboxPhases.Ending
                        or EvaluationTelemetryOutboxPhases.Accepted
                        or EvaluationTelemetryOutboxPhases.Committed
                    && existingOwner.TryConsumeRestoredTerminalAdoption())
                {
                    return proposedSession with
                    {
                        SessionId = existingOwner.SessionId,
                        RestoredTerminal = new EvaluationSessionRestoredTerminal(
                            existingOwner.TerminalAcceptance.Task,
                            existingCheckpoint.TerminalIntent!.TerminationClassification
                                .ParseParticipantTaskTerminationClassification()),
                    };
                }

                throw new EvaluationSessionAlreadyActiveException();
            }
        }

        var lease = await _exclusiveLease.TryAcquireAsync(leaseKey, cancellationToken).ConfigureAwait(false);
        if (lease is null)
        {
            throw new EvaluationSessionAlreadyActiveException();
        }

        var retainLease = false;
        try
        {
            if (!ShouldCaptureCurrentRun(identity))
            {
                throw new InvalidOperationException("The task capture context changed while acquiring its lease.");
            }

            EvaluationTelemetryOutboxCheckpoint? restoredCheckpoint;
            try
            {
                restoredCheckpoint = LoadIdentityCheckpoint(identity);
            }
            catch (Exception exception) when (exception is JsonException or NotSupportedException)
            {
                var quarantineFailure = QuarantineIdentity(leaseKey, exception);
                throw quarantineFailure;
            }
            if (restoredCheckpoint is not null)
            {
                if (restoredCheckpoint.Phase == EvaluationTelemetryOutboxPhases.Faulted)
                {
                    throw new EvaluationTelemetryDeliveryRejectedException(restoredCheckpoint.PermanentFailureStatusCode);
                }

                restoredCheckpoint = CloseRestoredActiveCheckpointAsAbandoned(restoredCheckpoint);
                var restoredOwner = RegisterTaskOwner(
                    leaseKey,
                    restoredCheckpoint,
                    lease,
                    restoredTerminalAdoptionAvailable: true);
                retainLease = true;
                StartTaskOwner(restoredOwner);
                if (restoredCheckpoint.Phase is EvaluationTelemetryOutboxPhases.Ending
                        or EvaluationTelemetryOutboxPhases.Accepted
                        or EvaluationTelemetryOutboxPhases.Committed
                    && restoredOwner.TryConsumeRestoredTerminalAdoption())
                {
                    return proposedSession with
                    {
                        SessionId = restoredOwner.SessionId,
                        RestoredTerminal = new EvaluationSessionRestoredTerminal(
                            restoredOwner.TerminalAcceptance.Task,
                            restoredCheckpoint.TerminalIntent!.TerminationClassification
                                .ParseParticipantTaskTerminationClassification()),
                    };
                }

                throw new EvaluationSessionAlreadyActiveException();
            }

            var checkpoint = _eventFactory.CreateTaskCheckpoint(proposedSession.SessionId, identity, taskStartPayload);
            lock (_gate)
            {
                Persist(checkpoint);
            }

            LogEnqueued(checkpoint.PendingEvents[0]);
            var owner = RegisterTaskOwner(
                leaseKey,
                checkpoint,
                lease,
                restoredTerminalAdoptionAvailable: false);
            retainLease = true;
            StartTaskOwner(owner);
            return proposedSession;
        }
        finally
        {
            if (!retainLease)
            {
                await lease.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    internal async Task<EvaluationSessionAcceptance?> RequestTaskEndAsync(
        EvaluationSession session,
        EvaluationTelemetryEnvelopeIdentity identity,
        TelemetryTaskEndOutcome outcome,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(outcome);

        EvaluationTelemetryStoredEvent? terminalEvent = null;
        EvaluationTelemetryTaskOwner owner;
        lock (_gate)
        {
            if (_suppressedRuns.Remove(session.SessionId))
            {
                return null;
            }

            owner = FindTaskOwner(session.SessionId)!;
            if (owner is null)
            {
                var unownedCheckpoint = FindTaskCheckpoint(session.SessionId);
                if (unownedCheckpoint?.Phase == EvaluationTelemetryOutboxPhases.Faulted)
                {
                    throw new EvaluationTelemetryDeliveryRejectedException(unownedCheckpoint.PermanentFailureStatusCode);
                }

                throw new InvalidOperationException("The evaluation task does not own its identity lease.");
            }
            var checkpoint = _checkpoints[owner.StorageKey];
            EnsureSessionBoundary(checkpoint, identity);
            if (checkpoint.Phase == EvaluationTelemetryOutboxPhases.Faulted)
            {
                throw new EvaluationTelemetryDeliveryRejectedException(checkpoint.PermanentFailureStatusCode);
            }

            if (checkpoint.Phase != EvaluationTelemetryOutboxPhases.Active)
            {
                throw new InvalidOperationException("Evaluation task completion was already requested.");
            }

            checkpoint = checkpoint with
            {
                Phase = EvaluationTelemetryOutboxPhases.Ending,
                TerminalIntent = outcome,
            };
            if (checkpoint.PendingEvents.Count == 0)
            {
                (checkpoint, terminalEvent) = _eventFactory.AppendTerminalEvent(checkpoint);
            }

            Persist(checkpoint);
        }

        if (terminalEvent is not null)
        {
            LogEnqueued(terminalEvent);
        }

        SignalCheckpoint(owner.StorageKey);
        return await owner.TerminalAcceptance.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task CommitTaskAsync(
        EvaluationSessionAcceptance acceptance,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(acceptance);
        cancellationToken.ThrowIfCancellationRequested();

        EvaluationTelemetryTaskOwner owner;
        var resumeCommittedOwner = false;
        lock (_gate)
        {
            owner = FindTaskOwner(acceptance.SessionId)
                ?? throw new InvalidOperationException("The accepted evaluation task no longer owns its identity lease.");
            var checkpoint = _checkpoints[owner.StorageKey];
            if (checkpoint.Phase is not (EvaluationTelemetryOutboxPhases.Accepted or EvaluationTelemetryOutboxPhases.Committed)
                || checkpoint.AcceptedReceipt is null
                || !string.Equals(checkpoint.SessionId, acceptance.SessionId, StringComparison.Ordinal)
                || !string.Equals(checkpoint.AcceptedReceipt.EventId, acceptance.EventId, StringComparison.Ordinal)
                || !string.Equals(checkpoint.AcceptedReceipt.BodySha256, acceptance.BodySha256, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The application commit does not match the durable accepted receipt.");
            }

            try
            {
                if (checkpoint.Phase == EvaluationTelemetryOutboxPhases.Accepted)
                {
                    Persist(checkpoint with { Phase = EvaluationTelemetryOutboxPhases.Committed });
                }
                else
                {
                    resumeCommittedOwner = true;
                }
            }
            catch (Exception exception) when (exception is IOException or InvalidOperationException)
            {
                throw new EvaluationLocalCommitUnavailableException(exception);
            }
        }

        if (resumeCommittedOwner)
        {
            StartTaskOwner(owner);
        }

        SignalCheckpoint(owner.StorageKey);
        await owner.LocalCommit.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
}
