namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Gemeinsame öffentliche Fehlercodes der Telemetrie-Endpunkte
/// in Forms- und Voice-Host.
/// </summary>
public static class TelemetryIngestionErrorCodes
{
    public const string PayloadInvalid = "E_TELEMETRY_PAYLOAD_INVALID";
    public const string PayloadTooLarge = "E_TELEMETRY_PAYLOAD_TOO_LARGE";
    public const string EventConflict = "E_TELEMETRY_EVENT_CONFLICT";
    public const string StoreUnavailable = "E_TELEMETRY_STORE_UNAVAILABLE";
}
