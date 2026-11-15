namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Festgelegte Endzustände einer aufgezeichneten Studienaufgabe.
/// </summary>
public static class TelemetryTaskEndStates
{
    /// <summary>
    /// Zielzustand der Aufgabe erreicht, ohne den gesamten Prozess abzusenden.
    /// </summary>
    public const string TaskTargetReached = "task_target_reached";

    /// <summary>
    /// Gesamter Prozess abgesendet. Dieser Zustand gehört zur abschließenden Aufgabe.
    /// </summary>
    public const string ProcessSubmitted = "process_submitted";

    /// <summary>
    /// Ausdrücklich angeordneter Studienabbruch, der im Beobachtungsblatt festgehalten wird.
    /// Der elektronische Wert übernimmt diese Entscheidung.
    /// </summary>
    public const string Aborted = "aborted";

    /// <summary>
    /// Aufgabe ohne erreichten Zielzustand und ohne angeordneten Studienabbruch beendet.
    /// </summary>
    public const string NotSubmitted = "not_submitted";

    /// <summary>
    /// Aufgabe aufgrund eines technischen Fehlers beendet.
    /// </summary>
    public const string TechnicalFailure = "technical_failure";

    /// <summary>
    /// Alle Endzustände aus der Studienereignisspezifikation.
    /// Erreichtes Aufgabenziel und abgesendeter Prozess bleiben getrennt,
    /// da nur die abschließende Aufgabe den Prozess absendet.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        TaskTargetReached,
        ProcessSubmitted,
        Aborted,
        NotSubmitted,
        TechnicalFailure,
    };
}
