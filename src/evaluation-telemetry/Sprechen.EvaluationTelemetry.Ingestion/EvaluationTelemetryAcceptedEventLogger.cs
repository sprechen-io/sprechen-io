using Microsoft.Extensions.Logging;

namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Protokolliert die Felder eines bereits angenommenen Ereignisses
/// mit dem gemeinsamen Nachrichtenschema.
/// </summary>
internal sealed class EvaluationTelemetryAcceptedEventLogger
{
    private readonly ILogger<EvaluationTelemetryAcceptedEventLogger> _logger;

    public EvaluationTelemetryAcceptedEventLogger(ILogger<EvaluationTelemetryAcceptedEventLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void LogAcceptedEnvelope(EvaluationTelemetryEnvelopeBuildResult envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var record = envelope.LogRecord;

        _logger.LogInformation(
            EvaluationTelemetryLogRecord.MessageTemplate,
            record.EventId,
            record.SessionId,
            record.ClientOffsetMs,
            record.EventName,
            record.ParticipantId,
            record.Condition,
            record.ApplicationId,
            record.TaskId,
            record.RecordProvenance,
            record.StateVersion,
            record.Data);
    }

}
