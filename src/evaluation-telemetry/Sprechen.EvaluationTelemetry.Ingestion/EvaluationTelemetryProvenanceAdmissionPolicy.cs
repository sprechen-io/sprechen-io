using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Trennt öffentliche Ereignisse von Testereignissen anhand ihrer Herkunft.
/// Der Annahmedienst prüft die Nutzlast vor diesem Aufruf gegen das Schema.
/// </summary>
internal static class EvaluationTelemetryProvenanceAdmissionPolicy
{
    public static bool IsAllowed(
        EvaluationTelemetryAdmissionTrustClass trustClass,
        string recordProvenance)
    {
        return trustClass switch
        {
            EvaluationTelemetryAdmissionTrustClass.Public => !string.Equals(
                recordProvenance,
                TelemetryRecordProvenances.OperationalProbe,
                StringComparison.Ordinal),
            EvaluationTelemetryAdmissionTrustClass.OperationalProbe =>
                string.Equals(
                    recordProvenance,
                    TelemetryRecordProvenances.OperationalProbe,
                    StringComparison.Ordinal),
            _ => throw new ArgumentOutOfRangeException(
                nameof(trustClass),
                trustClass,
                "Unsupported telemetry admission trust class."),
        };
    }
}
