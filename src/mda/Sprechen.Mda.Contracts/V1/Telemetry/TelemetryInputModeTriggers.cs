namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Festgelegte Auslöser eines Eingabemoduswechsels für die Telemetrie.
/// </summary>
public static class TelemetryInputModeTriggers
{
    /// <summary>
    /// Wechsel durch ein ausgesprochenes Kommando.
    /// </summary>
    public const string ExplicitCommand = "explicit_command";
    /// <summary>
    /// Automatischer Wechsel durch die Anwendung.
    /// </summary>
    public const string Automatic = "automatic";
    /// <summary>
    /// Wechsel aufgrund der Vorgabe für das Feld.
    /// </summary>
    public const string FieldDefault = "field_default";

    /// <summary>
    /// Alle zulässigen Auslöser für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ExplicitCommand,
        Automatic,
        FieldDefault,
    };
}
