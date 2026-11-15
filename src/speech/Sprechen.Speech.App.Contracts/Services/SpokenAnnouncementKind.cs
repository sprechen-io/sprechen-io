namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Grobe Kategorie einer gesprochenen Ansage für die Telemetrie.</summary>
/// <remarks>
/// Die Kategorie beschreibt den Zweck des Dialogschritts statt des gesprochenen Texts.
/// Dadurch muss für diese Unterscheidung kein Ansagetext aufgezeichnet werden, der
/// möglicherweise zuvor eingegebene Inhalte enthält.
/// </remarks>
public enum SpokenAnnouncementKind
{
    /// <summary>Allgemeine Ansage ohne speziellere Kategorie.</summary>
    Prompt = 0,

    /// <summary>
    /// Meldet, dass keine Eingabe erkannt wurde.
    /// </summary>
    NoInput = 1,

    /// <summary>
    /// Meldet einen Erkennungs- oder Erfassungsfehler mit Möglichkeit eines erneuten Versuchs.
    /// </summary>
    RecoverableFailure = 2,
}
