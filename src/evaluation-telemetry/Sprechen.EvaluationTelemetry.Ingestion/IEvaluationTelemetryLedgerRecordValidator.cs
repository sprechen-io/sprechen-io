namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Prüft gespeicherte Zeilen und liefert den kanonischen Datensatz
/// oder einen konkreten Ablehnungsgrund.
/// </summary>
public interface IEvaluationTelemetryLedgerRecordValidator
{
    EvaluationTelemetryLedgerLineReading ReadCanonicalLine(string jsonLine);
}
