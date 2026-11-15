namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Legt fest, ob die Mikrofonaufnahme während der Assistentenausgabe gesperrt
/// werden soll. HardOff fordert die Sperre an. Heuristic lässt die Aufnahme
/// offen und überlässt die Abgrenzung der heuristischen Verarbeitung.
/// </summary>
public enum MicGatePolicy
{
    Heuristic = 0,

    HardOff = 1,
}
