using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Legt die kanonische Studienbedingung des Hosts für eingehende Telemetrie fest.
/// </summary>
public sealed class EvaluationTelemetryIngressCondition
{
    public EvaluationTelemetryIngressCondition(StudyCondition value)
    {
        if (value is not (StudyCondition.A or StudyCondition.B))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ingress condition.");
        }

        Value = value;
        CanonicalValue = value.ToString();
    }

    public StudyCondition Value { get; }

    public string CanonicalValue { get; }
}
