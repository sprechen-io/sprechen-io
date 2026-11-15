namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Verarbeitet die geschlossene Familie <see cref="WakeStitchResult"/>, damit Verbraucher Ergebnisse ohne eigene Typverzweigungen ableiten können.
/// </summary>
/// <typeparam name="TResult">Das vom jeweiligen Verbraucher abgeleitete Ergebnis.</typeparam>
public interface IWakeStitchResultVisitor<out TResult>
{
    /// <summary>Verarbeitet die Verbindung mit einer vorherigen Teiläußerung.</summary>
    TResult VisitAttached(WakeStitchAttachedResult result);

    /// <summary>Verarbeitet die für den nächsten Zyklus vorgemerkte Teiläußerung.</summary>
    TResult VisitStarted(WakeStitchStartedResult result);

    /// <summary>Verarbeitet eine nach Ablauf des Zeitfensters verworfene Teiläußerung.</summary>
    TResult VisitExpired(WakeStitchExpiredResult result);

    /// <summary>
    /// Verarbeitet einen Zyklus ohne neu begonnene oder erfolgreiche Verknüpfung.
    /// </summary>
    TResult VisitNoOp(WakeStitchNoOpResult result);
}
