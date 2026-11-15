namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Festgelegte Arten von Mehrdeutigkeit für eine einheitliche Auswertung.
/// </summary>
public static class TelemetryAmbiguityTypes
{
    /// <summary>
    /// Mehrdeutigkeit zwischen einer Werteingabe und einem Kommando.
    /// </summary>
    public const string InputVsCommand = "input_vs_command";
    /// <summary>
    /// Mehrdeutigkeit bei einer buchstabierten Eingabe.
    /// </summary>
    public const string Spelling = "spelling";
    /// <summary>
    /// Mehrdeutigkeit zwischen einem Feldnamen und gesprochenem Feldinhalt.
    /// </summary>
    public const string FieldNameVsContent = "fieldname_vs_content";

    /// <summary>
    /// Alle zulässigen Mehrdeutigkeitsarten für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        InputVsCommand,
        Spelling,
        FieldNameVsContent,
    };
}
