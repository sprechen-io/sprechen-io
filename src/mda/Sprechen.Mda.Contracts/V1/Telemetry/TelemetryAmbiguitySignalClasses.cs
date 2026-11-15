namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Festgelegte Signalklassen zur Beschreibung der Eindeutigkeit einer Eingabe.
/// </summary>
public static class TelemetryAmbiguitySignalClasses
{
    /// <summary>
    /// Signal als eindeutig eingestuft.
    /// </summary>
    public const string Clear = "clear";
    /// <summary>
    /// Signal weiterhin als mehrdeutig eingestuft.
    /// </summary>
    public const string Ambiguous = "ambiguous";
    /// <summary>
    /// Signal nicht als Kommando eingestuft.
    /// </summary>
    public const string NoCommand = "no_command";

    /// <summary>
    /// Alle zulässigen Signalklassen für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Clear,
        Ambiguous,
        NoCommand,
    };
}
