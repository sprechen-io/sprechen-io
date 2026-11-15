namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Gemeinsame ILogger-Nachrichtenvorlage für evaluation_event. Die Platzhalternamen
/// müssen den Mitgliedern des Ereignisumschlags entsprechen, weil sie als
/// Eigenschaftsnamen exportiert werden. Die gemeinsame Definition verhindert
/// abweichende Vorlagen.
/// </summary>
public static class EvaluationTelemetryLogTemplate
{
    /// <summary>
    /// Platzhalter der Umschlagmitglieder in der Reihenfolge der Protokollierungsargumente.
    /// So können verschiedene Ereignisnamen dieselben Mitglieder nutzen, ohne deren
    /// Reihenfolge mehrfach zu pflegen.
    /// </summary>
    public const string EnvelopeMemberPlaceholders =
        "{event_id} {session_id} {client_offset_ms} {event} {participant_id} {condition} {application_id} {task_id} {record_provenance} {state_version} {data}";

    public const string MessageTemplate = "evaluation_event " + EnvelopeMemberPlaceholders;
}
