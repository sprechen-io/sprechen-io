namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Bestimmt, ob die Keyterms einer Anfrage den profilabhängigen Wortschatz
/// des Sprachdienstes ergänzen oder ohne diese Serverbasis verwendet werden.
/// </summary>
public enum KeytermComposition
{
    /// <summary>Vereinigt die Serverbasis mit den Keyterms der Anfrage. Dies ist der Standardwert.</summary>
    WithServerBase = 0,

    /// <summary>
    /// Verwendet nur die Keyterms der Anfrage. Eine leere Liste bedeutet hier
    /// keine Keyterms, während sie mit Serverbasis deren Wortschatz erhalten würde.
    /// </summary>
    WithoutServerBase = 1
}
