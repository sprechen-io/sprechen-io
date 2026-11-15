namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Erkennt die Routen, die den Diagnosebetrieb kennzeichnen.
/// </summary>
public static class DiagnosticPath
{
    public const string Segment = "debug";

    /// <summary>
    /// Erkennt <c>/debug</c> und Pfade mit <c>/debug/umfrage</c> als Anfang.
    /// Andere zweite Segmente und Pfade ohne <c>debug</c>-Präfix werden abgelehnt.
    /// Groß- und Kleinschreibung wird nicht unterschieden.
    /// </summary>
    public static bool IsMatch(string absolutePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);

        var segments = absolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0
            || !string.Equals(segments[0], Segment, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (segments.Length == 1)
        {
            return true;
        }

        return string.Equals(segments[1], "umfrage", StringComparison.OrdinalIgnoreCase);
    }
}
