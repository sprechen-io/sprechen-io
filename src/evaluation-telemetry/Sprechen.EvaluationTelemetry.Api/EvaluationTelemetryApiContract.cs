namespace Sprechen.EvaluationTelemetry.Api;

/// <summary>
/// Gemeinsame Routen und HTTP-Header für die Telemetrie-Endpunkte
/// der Forms- und Voice-Hosts.
/// </summary>
public static class EvaluationTelemetryApiContract
{
    public const string ArchiveReadKeyHeaderName = "X-Evaluation-Archive-Key";
    public const string ArchiveRoute = "/api/telemetry/archive";
    public const string IngestionRoute = "/api/telemetry";
    public const string OperationalProbeIngestionRoute = "/api/telemetry/probe";
    public const string OperationalProbeContextRoute = "/api/telemetry/probe/context";
}
