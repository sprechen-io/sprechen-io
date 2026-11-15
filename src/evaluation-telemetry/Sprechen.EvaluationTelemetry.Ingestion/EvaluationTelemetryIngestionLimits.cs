namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Legt die maximale Größe eines Telemetrie-Umschlags fest,
/// um den Speicherbedarf pro eingehender Anfrage zu begrenzen.
/// </summary>
public static class EvaluationTelemetryIngestionLimits
{
    public const int MaximumEnvelopeBytes = 64 * 1024;
}
