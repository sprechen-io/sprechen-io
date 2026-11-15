namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Definiert die gespeicherten Outbox-Phasen für Aktualisierung und Wiederanlaufprüfung. Den Wechsel zwischen den Phasen steuert der Aufgabenablauf.
/// </summary>
internal static class EvaluationTelemetryOutboxPhases
{
    internal const string Active = "active";
    internal const string Ending = "ending";
    internal const string Accepted = "accepted";
    internal const string Committed = "committed";
    internal const string OutsideTask = "outside_task";
    internal const string Faulted = "faulted";

    internal static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Active,
        Ending,
        Accepted,
        Committed,
        OutsideTask,
        Faulted,
    };
}
