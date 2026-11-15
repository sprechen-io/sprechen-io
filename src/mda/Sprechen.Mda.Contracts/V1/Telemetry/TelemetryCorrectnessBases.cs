namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Evidenzarten der Korrektheitsangabe auf dem Abschlussereignis. Die Laufzeit
/// sendet nur den systemseitigen Abgleich.
/// </summary>
public static class TelemetryCorrectnessBases
{
    public const string LogAndSystemCheck = "log_and_system_check";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        LogAndSystemCheck,
    };
}
