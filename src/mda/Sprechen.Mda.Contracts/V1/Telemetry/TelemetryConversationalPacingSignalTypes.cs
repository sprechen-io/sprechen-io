namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Festgelegte Ereignisarten zur Beschreibung des Gesprächsverlaufs.
/// </summary>
public static class TelemetryConversationalPacingSignalTypes
{
    /// <summary>
    /// Abgeschlossener Versuch einer Feldeingabe.
    /// </summary>
    public const string FieldAttempt = "field_attempt";
    /// <summary>
    /// Antwortfenster ohne verwertbare Sprache.
    /// </summary>
    public const string NoInput = "no_input";
    /// <summary>
    /// Korrektur- oder Hilfeversuch nach einem Eingabeproblem.
    /// </summary>
    public const string RepairAttempt = "repair_attempt";
    /// <summary>
    /// Ausdrücklich angeforderte Hilfe.
    /// </summary>
    public const string HelpRequest = "help_request";
    /// <summary>
    /// Ausdrücklich angeforderte Wiederholung.
    /// </summary>
    public const string RepeatRequest = "repeat_request";
    /// <summary>
    /// Hineinsprechen während einer Ansage.
    /// </summary>
    public const string BargeIn = "barge_in";
    /// <summary>
    /// Erneutes Antwortfenster für dasselbe Feld.
    /// </summary>
    public const string CaptureRetry = "capture_retry";
    /// <summary>
    /// Gesprochenes Kommando abgelehnt oder nicht erkannt.
    /// </summary>
    public const string CommandRecognitionFailure = "command_recognition_failure";

    /// <summary>
    /// Alle zulässigen Ereignisarten für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        FieldAttempt,
        NoInput,
        RepairAttempt,
        HelpRequest,
        RepeatRequest,
        BargeIn,
        CaptureRetry,
        CommandRecognitionFailure,
    };
}
