namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Festgelegte Eingabemodi für die Telemetrie.
/// </summary>
public static class TelemetryInputModes
{
    /// <summary>
    /// Eingabe durch gesprochene Sprache.
    /// </summary>
    public const string Voice = "voice";
    /// <summary>
    /// Eingabe über die Tastatur.
    /// </summary>
    public const string Keyboard = "keyboard";
    /// <summary>
    /// Eingabe durch Buchstabieren.
    /// </summary>
    public const string Spelling = "spelling";
    /// <summary>
    /// Eingabe einzelner Ziffern.
    /// </summary>
    public const string Digits = "digits";
    /// <summary>
    /// Wörtliche Eingabe.
    /// </summary>
    public const string Literal = "literal";

    /// <summary>
    /// Alle zulässigen Eingabemodi für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Voice,
        Keyboard,
        Spelling,
        Digits,
        Literal,
    };
}
