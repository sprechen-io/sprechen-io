namespace Sprechen.Speech.Runtime.Tests.Capture;

/// <summary>
/// Gemeinsame Abschlussereignisse für den Vergleich beider Aufnahmewege.
/// </summary>
public enum CaptureTerminalProbe
{
    /// <summary>Der Versuch wird mit einer Meldung über fehlende Sprache beendet.</summary>
    NoSpeech,

    /// <summary>Ein technischer Fehler beendet den Versuch.</summary>
    Failure,

    /// <summary>Der Aufrufer bricht den Versuch ab.</summary>
    Cancellation,
}
