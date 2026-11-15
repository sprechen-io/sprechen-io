namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Verbindet akustische Evidenz, lexikalische Auswertung, Verknüpfungsergebnis
/// und Ersatzgrund deterministisch zu einer Aktivierungsentscheidung.
/// Die Entscheidung erfolgt ohne Ein- und Ausgabe oder Telemetrie.
/// </summary>
public interface IWakeArbitrator
{
    /// <summary>
    /// Bestimmt das Ergebnis für die übergebenen Eingaben. Null wird mit
    /// <see cref="ArgumentNullException"/> abgewiesen, strukturell ungültige
    /// Kombinationen mit <see cref="ArgumentException"/>, etwa Ersatzpfad und
    /// gleichzeitige Detektionsevidenz.
    /// </summary>
    WakeArbitrationOutcome Arbitrate(WakeArbitrationInput input);
}
