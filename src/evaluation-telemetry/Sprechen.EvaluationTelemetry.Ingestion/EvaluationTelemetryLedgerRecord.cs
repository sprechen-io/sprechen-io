namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Enthält die kanonische JSONL-Zeile eines angenommenen Ereignisses
/// und die Kennungen für Speicherung und Wiederherstellung.
/// </summary>
public sealed record EvaluationTelemetryLedgerRecord(
    string EventId,
    string ParticipantId,
    string TaskId,
    string Condition,
    string JsonLine);
