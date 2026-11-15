namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Festgelegte Arten von Systemrückmeldungen für die Telemetrie.
/// </summary>
public static class TelemetrySystemFeedbackTypes
{
    public const string Info = "info";
    public const string Error = "error";
    public const string Confirm = "confirm";
    public const string Help = "help";
    /// <summary>
    /// Rückmeldung zum aktuellen Stand.
    /// </summary>
    public const string Status = "status";
    /// <summary>
    /// Sprachliche Navigation ohne gefundenes Ziel. Diese Diagnose gehört nur zu Bedingung B.
    /// Der optionale Eintrag <c>command_id</c> stellt den Kommandobezug her.
    /// </summary>
    public const string NavigationNoTarget = "navigationNoTarget";

    /// <summary>
    /// Alle zulässigen Rückmeldungsarten für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Info,
        Error,
        Confirm,
        Help,
        Status,
        NavigationNoTarget,
    };
}
