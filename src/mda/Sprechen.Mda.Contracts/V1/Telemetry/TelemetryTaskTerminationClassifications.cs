namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Im Modell festgelegte Gründe für das Beenden einer Aufgabe durch die Anwendung.
/// </summary>
public static class TelemetryTaskTerminationClassifications
{
    public const string Completed = "completed";
    public const string ParticipantCancelled = "participant_cancelled";
    public const string SessionAbandoned = "session_abandoned";
    public const string StudyDirectedAbort = "study_directed_abort";
    public const string TechnicalFailure = "technical_failure";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Completed,
        ParticipantCancelled,
        SessionAbandoned,
        StudyDirectedAbort,
        TechnicalFailure,
    };
}
