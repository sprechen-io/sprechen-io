namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Trennt die lexikalische Aktivierung von ihrer ergänzenden Diagnose, damit beide Ereignisse eindeutig zugeordnet werden können.
/// </summary>
public enum WakeWordActivationPhase
{
    LexicalDetected = 0,
    Diagnostic = 1
}
