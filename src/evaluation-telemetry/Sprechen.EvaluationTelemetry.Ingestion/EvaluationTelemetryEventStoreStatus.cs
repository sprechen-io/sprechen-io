namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Nennt, ob das Schreiben in den Speicher gelang, damit die Aufnahme zwischen
/// Konflikt und Ausfall unterscheiden kann.
/// </summary>
public enum EvaluationTelemetryEventStoreStatus
{
    Persisted,
    AlreadyPresent,
    Conflicting,
    Unavailable,
}
