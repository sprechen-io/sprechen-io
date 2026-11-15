namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Enthält die kanonische JSONL-Zeile mit ihrer Identität oder den
/// Fehlercode einer abgelehnten Nutzlast.
/// </summary>
internal sealed class EvaluationTelemetryEnvelopeBuildResult
{
    private readonly EvaluationTelemetryLogRecord? _logRecord;

    private EvaluationTelemetryEnvelopeBuildResult(
        bool accepted,
        string errorCode,
        string jsonLine,
        string eventId,
        string participantId,
        string taskId,
        string condition,
        EvaluationTelemetryLogRecord? logRecord)
    {
        Accepted = accepted;
        ErrorCode = errorCode;
        JsonLine = jsonLine;
        EventId = eventId;
        ParticipantId = participantId;
        TaskId = taskId;
        Condition = condition;
        _logRecord = logRecord;
    }

    public bool Accepted { get; }

    /// <summary>Die ablehnende Regel, <c>telemetry.validation.none</c> bei Annahme.</summary>
    public string ErrorCode { get; }

    public string JsonLine { get; }

    public string EventId { get; }

    public string ParticipantId { get; }

    public string TaskId { get; }

    public string Condition { get; }

    public EvaluationTelemetryLogRecord LogRecord => _logRecord
        ?? throw new InvalidOperationException("A refused evaluation telemetry payload has no log record.");

    public static EvaluationTelemetryEnvelopeBuildResult Accept(
        string jsonLine,
        string eventId,
        string participantId,
        string taskId,
        string condition,
        EvaluationTelemetryLogRecord logRecord)
    {
        ArgumentNullException.ThrowIfNull(logRecord);

        return new EvaluationTelemetryEnvelopeBuildResult(
            accepted: true,
            errorCode: EvaluationTelemetryValidationErrorCodes.None,
            jsonLine: jsonLine,
            eventId: eventId,
            participantId: participantId,
            taskId: taskId,
            condition: condition,
            logRecord: logRecord);
    }

    public static EvaluationTelemetryEnvelopeBuildResult Reject(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            throw new ArgumentException("A refused telemetry payload requires an error code.", nameof(errorCode));
        }

        return new EvaluationTelemetryEnvelopeBuildResult(
            accepted: false,
            errorCode: errorCode,
            jsonLine: string.Empty,
            eventId: string.Empty,
            participantId: string.Empty,
            taskId: string.Empty,
            condition: string.Empty,
            logRecord: null);
    }
}
