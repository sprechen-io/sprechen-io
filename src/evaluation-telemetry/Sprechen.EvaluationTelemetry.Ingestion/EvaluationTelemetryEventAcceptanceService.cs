using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Prüft eingehende Ereignisse und bestätigt sie erst nach erfolgreicher Speicherung.
/// Validierungsfehler bleiben im Serverprotokoll.
/// </summary>
internal sealed class EvaluationTelemetryEventAcceptanceService : IEvaluationTelemetryEventAcceptanceService
{
    private readonly IEvaluationTelemetryEventStore _eventStore;
    private readonly TelemetryEnvelopeSchemaValidator _schemaValidator;
    private readonly EvaluationTelemetryEnvelopeBuilder _envelopeBuilder;
    private readonly EvaluationTelemetryIngressCondition _ingressCondition;
    private readonly EvaluationTelemetryAcceptedEventLogger _acceptedEventLogger;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EvaluationTelemetryEventAcceptanceService> _logger;

    public EvaluationTelemetryEventAcceptanceService(
        IEvaluationTelemetryEventStore eventStore,
        TelemetryEnvelopeSchemaValidator schemaValidator,
        EvaluationTelemetryEnvelopeBuilder envelopeBuilder,
        EvaluationTelemetryIngressCondition ingressCondition,
        EvaluationTelemetryAcceptedEventLogger acceptedEventLogger,
        TimeProvider timeProvider,
        ILogger<EvaluationTelemetryEventAcceptanceService> logger)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
        _envelopeBuilder = envelopeBuilder ?? throw new ArgumentNullException(nameof(envelopeBuilder));
        _ingressCondition = ingressCondition ?? throw new ArgumentNullException(nameof(ingressCondition));
        _acceptedEventLogger = acceptedEventLogger ?? throw new ArgumentNullException(nameof(acceptedEventLogger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<EvaluationTelemetryEventAcceptanceResult> AcceptPublicAsync(
        Stream requestBody,
        CancellationToken cancellationToken = default)
    {
        return AcceptAsync(requestBody, EvaluationTelemetryAdmissionTrustClass.Public, cancellationToken);
    }

    public Task<EvaluationTelemetryEventAcceptanceResult> AcceptOperationalProbeAsync(
        Stream requestBody,
        CancellationToken cancellationToken = default)
    {
        return AcceptAsync(requestBody, EvaluationTelemetryAdmissionTrustClass.OperationalProbe, cancellationToken);
    }

    private async Task<EvaluationTelemetryEventAcceptanceResult> AcceptAsync(
        Stream requestBody,
        EvaluationTelemetryAdmissionTrustClass admissionTrustClass,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestBody);

        TelemetryEventPayload? payload;
        try
        {
            await using var boundedBody = new MemoryStream();
            var buffer = new byte[8192];
            while (true)
            {
                var bytesRead = await requestBody.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                if (boundedBody.Length + bytesRead > EvaluationTelemetryIngestionLimits.MaximumEnvelopeBytes)
                {
                    return TooLarge();
                }

                await boundedBody.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken)
                    .ConfigureAwait(false);
            }

            var serializedEnvelope = System.Text.Encoding.UTF8.GetString(boundedBody.ToArray());
            var refusal = _schemaValidator.DescribeRefusal(serializedEnvelope);
            if (refusal.Length > 0)
            {
                // Der Aufrufer erhält nur den allgemeinen Ungültigkeitscode.
                // Der konkrete Ablehnungsgrund bleibt für die Diagnose im Serverprotokoll.
                _logger.LogWarning("Refused an evaluation telemetry envelope: {Refusal}", refusal);
                return Invalid();
            }

            boundedBody.Position = 0;
            payload = await JsonSerializer.DeserializeAsync<TelemetryEventPayload>(
                boundedBody,
                TelemetryEventPayload.ReadOptions,
                cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return Invalid();
        }

        if (payload is null)
        {
            return Invalid();
        }

        if (!string.Equals(payload.Condition, _ingressCondition.CanonicalValue, StringComparison.Ordinal))
        {
            return Invalid();
        }

        if (!EvaluationTelemetryProvenanceAdmissionPolicy.IsAllowed(
                admissionTrustClass,
                payload.RecordProvenance))
        {
            return Invalid();
        }

        // Der Server setzt den Empfangszeitpunkt selbst in seiner Ortszeit.
        // Vom Absender mitgelieferte Zeitpunkte werden bereits bei der Schemaprüfung abgelehnt.
        var stampedPayload = payload with
        {
            RecordReceivedAt = _timeProvider.GetLocalNow().ToString(
                TelemetryRecordReceivedAt.Representation,
                CultureInfo.InvariantCulture),
        };

        EvaluationTelemetryEnvelopeBuildResult envelope;
        try
        {
            envelope = _envelopeBuilder.Build(stampedPayload);
        }
        catch (JsonException exception)
        {
            // Ein Fehler der erzeugten Zeile wird intern protokolliert.
            // Nach außen bleibt es beim allgemeinen Ungültigkeitscode.
            _logger.LogError(exception, "Built a non-canonical evaluation telemetry envelope from an accepted payload.");
            return Invalid();
        }

        if (!envelope.Accepted)
        {
            // Die Übergangsprüfung liefert ihren konkreten Ablehnungsgrund nur ins Serverprotokoll.
            _logger.LogWarning(
                "Refused an evaluation telemetry payload by {ErrorCode}.",
                envelope.ErrorCode);
            return Invalid();
        }

        EvaluationTelemetryEventStoreStatus storeStatus;
        try
        {
            storeStatus = await _eventStore.AcceptAsync(
                new EvaluationTelemetryLedgerRecord(
                    envelope.EventId,
                    envelope.ParticipantId,
                    envelope.TaskId,
                    envelope.Condition,
                    envelope.JsonLine),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist evaluation telemetry event {EventId}.", envelope.EventId);
            return Unavailable();
        }

        switch (storeStatus)
        {
            case EvaluationTelemetryEventStoreStatus.Persisted:
                _acceptedEventLogger.LogAcceptedEnvelope(envelope);
                return new EvaluationTelemetryEventAcceptanceResult(
                    EvaluationTelemetryEventAcceptanceStatus.Accepted);
            case EvaluationTelemetryEventStoreStatus.AlreadyPresent:
                return new EvaluationTelemetryEventAcceptanceResult(
                    EvaluationTelemetryEventAcceptanceStatus.AlreadyAccepted);
            case EvaluationTelemetryEventStoreStatus.Conflicting:
                return new EvaluationTelemetryEventAcceptanceResult(
                    EvaluationTelemetryEventAcceptanceStatus.Conflicting,
                    TelemetryIngestionErrorCodes.EventConflict);
            case EvaluationTelemetryEventStoreStatus.Unavailable:
                return Unavailable();
            default:
                throw new InvalidOperationException($"Unsupported event-store status '{storeStatus}'.");
        }
    }

    private static EvaluationTelemetryEventAcceptanceResult Invalid()
    {
        return new EvaluationTelemetryEventAcceptanceResult(
            EvaluationTelemetryEventAcceptanceStatus.Invalid,
            TelemetryIngestionErrorCodes.PayloadInvalid);
    }

    private static EvaluationTelemetryEventAcceptanceResult TooLarge()
    {
        return new EvaluationTelemetryEventAcceptanceResult(
            EvaluationTelemetryEventAcceptanceStatus.PayloadTooLarge,
            TelemetryIngestionErrorCodes.PayloadTooLarge);
    }

    private static EvaluationTelemetryEventAcceptanceResult Unavailable()
    {
        return new EvaluationTelemetryEventAcceptanceResult(
            EvaluationTelemetryEventAcceptanceStatus.Unavailable,
            TelemetryIngestionErrorCodes.StoreUnavailable);
    }
}
