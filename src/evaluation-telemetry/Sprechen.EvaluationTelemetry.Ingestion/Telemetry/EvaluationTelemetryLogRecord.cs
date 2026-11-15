using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Übernimmt die bereits geprüften Ereignisdaten für das strukturierte Betriebslog,
/// ohne ihre Werte nachträglich zu kürzen oder zu korrigieren.
/// </summary>
internal sealed class EvaluationTelemetryLogRecord
{
    // Die gemeinsame Vorlage hält die Eigenschaftsnamen im strukturierten Log einheitlich.
    public const string MessageTemplate = EvaluationTelemetryLogTemplate.MessageTemplate;

    public required string EventId { get; init; }

    public required string SessionId { get; init; }

    public required string ClientOffsetMs { get; init; }

    public required string EventName { get; init; }

    public required string ParticipantId { get; init; }

    public required string Condition { get; init; }

    public required string ApplicationId { get; init; }

    public required string TaskId { get; init; }

    public required string RecordProvenance { get; init; }

    public int StateVersion { get; init; }

    public required string Data { get; init; }

    public static EvaluationTelemetryLogRecord For(TelemetryEventPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return new EvaluationTelemetryLogRecord
        {
            EventId = payload.EventId,
            SessionId = payload.SessionId ?? string.Empty,
            ClientOffsetMs = payload.ClientOffsetMs?.ToString() ?? string.Empty,
            EventName = payload.Event,
            ParticipantId = payload.ParticipantId,
            Condition = payload.Condition,
            ApplicationId = payload.ApplicationId,
            TaskId = payload.TaskId,
            RecordProvenance = payload.RecordProvenance,
            StateVersion = payload.StateVersion,
            Data = payload.Data?.GetRawText() ?? throw new ArgumentException(
                "An accepted evaluation telemetry envelope carries a data object.",
                nameof(payload)),
        };
    }
}
