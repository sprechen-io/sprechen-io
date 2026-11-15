namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Veröffentlicht das Versionspräfix, mit dem Browseradapter die Checkpoints
/// der aktuellen Sprach-Outbox beim Aufzählen eingrenzen.
/// </summary>
public static class EvaluationTelemetryOutboxStorageContract
{
    /// <summary>
    /// Präfix der aktuellen Speicherform. Ältere Formen werden darüber nicht aufgezählt.
    /// </summary>
    public const string StorageKeyPrefix = "sprechen.evaluation.outbox.v1:";
}
