namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Wählt die Startposition eines neuen <see cref="SampleCursor"/> im Sample-Ring.
/// </summary>
public enum SampleCursorOrigin
{
    /// <summary>
    /// Beginnt bei den ältesten noch gespeicherten Daten: nach einem Überlauf bei
    /// <c>writeHead - capacity</c>, davor am Anfang des Stroms.
    /// </summary>
    Earliest = 0,

    /// <summary>
    /// Beginnt am aktuellen Schreibkopf. Lesen liefert damit nur Daten,
    /// die nach dem Öffnen dieses Cursors geschrieben wurden.
    /// </summary>
    Latest = 1,
}
