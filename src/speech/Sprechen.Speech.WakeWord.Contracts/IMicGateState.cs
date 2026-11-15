namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Stellt die Mikrofonsperre während der Assistentenausgabe bereit,
/// damit Aufnahmen die Freigabe abwarten können.
/// </summary>
public interface IMicGateState
{
    /// <summary>
    /// Zeigt die unter HardOff gesetzte Aufnahmesperre an. Sie bleibt nach
    /// Wiedergabeende bis zur Browserbestätigung oder zum fehlgeschlagenen Öffnen bestehen.
    /// </summary>
    bool IsHardOff { get; }

    /// <summary>
    /// Wartet auf die Freigabe nach der Wiedergabe und der Browserbestätigung.
    /// Ohne gesetzte Sperre endet der Aufruf sofort. Fehler beim Schließen oder
    /// Öffnen können das Warten mit einer Ausnahme beenden. Der Token bricht nur
    /// dieses Warten ab.
    /// </summary>
    ValueTask WaitUntilOpenAsync(CancellationToken cancellationToken);
}
