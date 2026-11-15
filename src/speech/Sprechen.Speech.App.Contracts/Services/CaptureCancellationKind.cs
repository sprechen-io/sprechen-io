namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Legt fest, ob ein Abbruch bisher erfasstes Audio zurückgibt,
/// die Aufnahme verwirft oder eine ausdrückliche Pause meldet.
/// </summary>
public enum CaptureCancellationKind
{
    /// <summary>
    /// Beendet die Erfassung mit dem bisher verfügbaren Audio. Für Aufrufer,
    /// die bei einem Moduswechsel ein Teilergebnis verwenden möchten.
    /// </summary>
    Drain = 0,

    /// <summary>
    /// Verwirft die laufende Erfassung ohne Audioergebnis, etwa beim
    /// Sitzungsende oder Entsorgen, wenn kein Teilergebnis mehr benötigt wird.
    /// </summary>
    Abort = 1,

    /// <summary>
    /// Beendet das Zuhören aufgrund einer ausdrücklichen Pause der Spracheingabe.
    /// Dialoge behandeln diesen Ausgang gesondert von Leerlauf und allgemeinem Abbruch.
    /// </summary>
    Paused = 2,
}
