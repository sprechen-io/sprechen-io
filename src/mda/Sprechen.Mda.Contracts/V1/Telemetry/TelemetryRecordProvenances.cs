namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Bündelt die geschlossenen Herkunftsklassen für Evaluationsdatensätze, damit Studienereignisse
/// und Testläufe anhand ihrer Herkunft unterschieden werden.
/// </summary>
public static class TelemetryRecordProvenances
{
    public const string Participant = "participant";

    public const string OperationalProbe = "operational_probe";

    public const string Demonstration = "demonstration";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Participant,
        OperationalProbe,
        Demonstration,
    };
}
