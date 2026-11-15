namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Art des Studienereignisses für einen internen Fehlercode der Sprachoberfläche.
/// Die ausdrückliche Zuordnung verhindert, dass neue interne Codes unbeabsichtigt
/// als zwischen A und B vergleichbare Fehler behandelt werden.
/// </summary>
public enum TelemetryStudyErrorRoute
{
    /// <summary>
    /// Geöffnete und geschlossene Validierungsfehlermeldung zu einer Regelverletzung
    /// oder einer entsprechenden Meldung der Sprachoberfläche.
    /// </summary>
    Validation,

    /// <summary>
    /// Erkennungsfehler, dessen Auftreten ohne Lesen des Transkripts belegt ist.
    /// </summary>
    Recognition,

    /// <summary>
    /// Ereignis für einen Gesprächsschritt ohne Sprache. Stille allein belegt keinen Erkennerausfall.
    /// </summary>
    Silence,

    /// <summary>
    /// Kein Studienfehlerereignis, da weder Regelverletzung noch Erkennerausfall belegt ist.
    /// </summary>
    None,
}
