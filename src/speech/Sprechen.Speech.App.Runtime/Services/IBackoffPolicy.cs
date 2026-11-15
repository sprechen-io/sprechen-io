namespace Sprechen.Speech.App.Runtime.Services;

/// <summary>
/// Liefert Versuchslimit und Wartezeiten für Erkennungswiederholungen, damit der ausführende Ablauf das Warten und seinen Abbruch selbst steuern kann.
/// Die Berechnung darf weder blockieren noch Streams anlegen oder die Systemzeit beobachten.
/// </summary>
public interface IBackoffPolicy
{
    /// <summary>
    /// Begrenzt die aufeinanderfolgenden technischen Fehlschläge eines Auftrags bis zur <see cref="SpeechRecognitionBackoffExhaustedException"/>.
    /// Jeder Aufruf des Erkennungsclients zählt als ein Versuch.
    /// </summary>
    int MaxRetries { get; }

    /// <summary>
    /// Berechnet die Wartezeit vor dem nächsten Versuch. Index <c>0</c> bezeichnet die Pause zwischen dem ersten Fehlschlag und dem zweiten Versuch.
    /// </summary>
    TimeSpan GetDelay(int attemptIndex);
}
