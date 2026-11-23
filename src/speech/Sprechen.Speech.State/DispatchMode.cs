namespace Sprechen.Speech.State;

/// <summary>
/// Steuert die Unterbrechung laufender Effekte beim Einreihen eines Ereignisses.
/// <see cref="Enqueue"/> reiht regulär ein. <see cref="Interrupt"/> reiht das Ereignis ein
/// und signalisiert dann den bisherigen Effekten den Abbruch. Bereits wartende Ereignisse
/// bleiben in ihrer Reihenfolge erhalten.
/// </summary>
public enum DispatchMode
{
    Enqueue = 0,
    Interrupt = 1
}


