namespace Sprechen.EvaluationTelemetry.Api;

/// <summary>
/// Steuert, ob Ereignisse aus Testläufen gespeichert werden.
/// </summary>
public sealed class EvaluationTelemetryOperationalProbeAdmissionOptions
{
    public const string SectionName = "EvaluationTelemetry:OperationalProbeAdmission";

    public bool IsEnabled { get; set; }
}
