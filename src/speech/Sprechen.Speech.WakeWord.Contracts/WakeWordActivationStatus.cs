namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Unterscheidet fehlenden erkannten Text, Text ohne führende Aktivierungsform
/// und eine erkannte Aktivierung für die anschließende Reaktion.
/// </summary>
public enum WakeWordActivationStatus
{
    NoRecognizedSpeech,
    MissingLeadingWakeWord,
    LeadingWakeWordDetected
}
