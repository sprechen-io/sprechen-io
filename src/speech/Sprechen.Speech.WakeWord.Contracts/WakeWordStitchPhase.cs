namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Unterscheidet Beginn, Verbindung, Ablauf und ausbleibende Verknüpfung.
/// </summary>
public enum WakeWordStitchPhase
{
    Started = 0,
    Attached = 1,
    Expired = 2,
    NoOp = 3
}
