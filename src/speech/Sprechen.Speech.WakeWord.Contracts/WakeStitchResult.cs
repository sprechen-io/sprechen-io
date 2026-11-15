namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Gemeinsame Basis der Ergebnisse beim Zusammenfügen von Aufnahmezyklen:
/// verbunden, vorgemerkt, abgelaufen oder ohne neue Verknüpfung.
/// </summary>
public abstract class WakeStitchResult
{
    private protected WakeStitchResult()
    {
    }

    /// <summary>
    /// Übergibt das Ergebnis an den Besucher, damit Verbraucher keine eigenen Untertypverzweigungen benötigen.
    /// </summary>
    public abstract TResult Accept<TResult>(IWakeStitchResultVisitor<TResult> visitor);
}
