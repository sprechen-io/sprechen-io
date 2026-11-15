using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Stellt gespeicherte Outbox-Stände unter ihren Identitätssperren wieder her.
/// Aktive Aufgaben werden mangels dauerhaftem Dialogzustand als verlassen beendet.
/// Unlesbare Identitäten bleiben getrennt gesperrt.
/// </summary>
internal sealed partial class EvaluationTelemetryTaskOutbox
{
    private Task EnsureInitializedAsync()
    {
        lock (_gate)
        {
            return _initializationTask ??= InitializeRestoredCheckpointsAsync();
        }
    }

    private async Task InitializeRestoredCheckpointsAsync()
    {
        var storageKeys = _store.ListKeys()
            .Where(static key => key.StartsWith(EvaluationTelemetryOutboxKeys.StorageKeyPrefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        foreach (var storageKey in storageKeys.Where(
                     static key => !EvaluationTelemetryOutboxKeys.TryReadTaskIdentity(key, out _, out _)))
        {
            if (!_operationalProbeSession.IsRequested && CanRestoreOutsideTask(storageKey))
            {
                StartOutsideTaskPump(storageKey);
            }
        }

        var taskIdentityGroups = storageKeys
            .Select(key => EvaluationTelemetryOutboxKeys.TryReadTaskIdentity(key, out var identity, out _)
                ? new { Key = key, Identity = identity }
                : null)
            .Where(candidate => candidate is not null && CanRestoreIdentity(candidate.Identity!))
            .GroupBy(
                static candidate => EvaluationTelemetryOutboxKeys.BuildIdentityLeaseKey(candidate!.Identity!),
                StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal);

        foreach (var identityGroup in taskIdentityGroups)
        {
            var identity = identityGroup.First()!.Identity!;
            var leaseKey = identityGroup.Key;

            var lease = await _exclusiveLease.TryAcquireAsync(leaseKey, _shutdown.Token).ConfigureAwait(false);
            if (lease is null)
            {
                continue;
            }

            var retainLease = false;
            try
            {
                if (!CanRestoreIdentity(identity))
                {
                    continue;
                }

                var matchingKeys = _store.ListKeys()
                    .Where(key => EvaluationTelemetryOutboxKeys.TryReadTaskIdentity(key, out var currentIdentity, out _)
                        && currentIdentity == identity)
                    .Order(StringComparer.Ordinal)
                    .ToArray();
                if (matchingKeys.Length == 0)
                {
                    continue;
                }

                if (matchingKeys.Length > 1)
                {
                    throw new JsonException("More than one evaluation checkpoint exists for the same task identity.");
                }

                var reloaded = ReloadCheckpoint(matchingKeys[0]);
                if (reloaded is null || reloaded.Phase == EvaluationTelemetryOutboxPhases.Faulted)
                {
                    continue;
                }

                reloaded = CloseRestoredActiveCheckpointAsAbandoned(reloaded);

                var owner = RegisterTaskOwner(
                    leaseKey,
                    reloaded,
                    lease,
                    restoredTerminalAdoptionAvailable: reloaded.Phase is
                        EvaluationTelemetryOutboxPhases.Ending or
                        EvaluationTelemetryOutboxPhases.Accepted or
                        EvaluationTelemetryOutboxPhases.Committed);
                retainLease = true;
                if (reloaded.Phase != EvaluationTelemetryOutboxPhases.Committed)
                {
                    StartTaskOwner(owner);
                }
            }
            catch (Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException or IOException)
            {
                var quarantineFailure = QuarantineIdentity(leaseKey, exception);
                _logger.LogError(
                    quarantineFailure,
                    "Evaluation checkpoint failed closed for identity {LeaseKey}; other identities remain available.",
                    leaseKey);
            }
            finally
            {
                if (!retainLease)
                {
                    await lease.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }

    private bool CanRestoreOutsideTask(string storageKey)
    {
        var serialized = _store.Read(storageKey);
        if (serialized is null)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(serialized);
            return !document.RootElement.TryGetProperty("identity", out var identity)
                || !identity.TryGetProperty("record_provenance", out var provenance)
                || provenance.GetString() != TelemetryRecordProvenances.OperationalProbe;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            // Unlesbare Daten prüft anschließend der reguläre Validator. Diese Herkunftsprüfung verwirft sie nicht stillschweigend.
            return true;
        }
    }

    private EvaluationTelemetryOutboxCheckpoint CloseRestoredActiveCheckpointAsAbandoned(
        EvaluationTelemetryOutboxCheckpoint checkpoint)
    {
        if (checkpoint.Phase != EvaluationTelemetryOutboxPhases.Active)
        {
            return checkpoint;
        }

        var abandoned = checkpoint with
        {
            Phase = EvaluationTelemetryOutboxPhases.Ending,
            TerminalIntent = TelemetryTaskEndProjection.Project(
                processSubmitted: false,
                requiredFieldsTotal: 0,
                requiredFieldsCompleted: 0,
                openBlockingValidationCount: 0,
                TelemetryTaskTerminationClassifications.SessionAbandoned),
        };
        EvaluationTelemetryStoredEvent? terminalEvent = null;
        if (abandoned.PendingEvents.Count == 0)
        {
            (abandoned, terminalEvent) = _eventFactory.AppendTerminalEvent(abandoned);
        }

        Persist(abandoned);
        if (terminalEvent is not null)
        {
            LogEnqueued(terminalEvent);
        }

        _logger.LogWarning(
            "Restored active evaluation run {SessionId} has no durable Voice dialog state; it is closed as session_abandoned before any new run may start.",
            checkpoint.SessionId);
        return abandoned;
    }
}
