namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Unterscheidet volle Warteschlange, ausgeschöpfte Wiederholungsversuche,
/// Mikrofonsperre und Weiterleitung, damit abgelehnte Erkennungen gezielt diagnostiziert werden können.
/// </summary>
public enum WakeWordTranscriptionRejectionPhase
{
    QueueFull = 0,
    BackoffExhausted = 1,
    MicGateHardOff = 2,
    Dispatched = 3
}
