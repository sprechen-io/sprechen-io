namespace Sprechen.Speech.State;

/// <summary>
/// Steuert die Unterbrechung laufender Effekte beim Einreihen eines Ereignisses.
/// <see cref="Enqueue"/> reiht regulär ein, und <see cref="Interrupt"/> signalisiert
/// den bisherigen Effekten den Abbruch und reiht danach ebenfalls in dieselbe Warteschlange ein.
/// Bereits wartende Ereignisse werden dadurch nicht überholt.
/// </summary>
public enum DispatchMode
{
    Enqueue = 0,
    Interrupt = 1
}


