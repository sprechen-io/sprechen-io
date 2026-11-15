namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Unterscheidet Herkunftsklassen, damit eine Route nur zugelassene Ursprünge
/// aufnimmt.
/// </summary>
internal enum EvaluationTelemetryAdmissionTrustClass
{
    Public,
    OperationalProbe,
}
