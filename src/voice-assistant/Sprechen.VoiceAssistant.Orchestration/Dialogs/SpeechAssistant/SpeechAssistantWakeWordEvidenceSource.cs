namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet lexikalische und akustische Herkunft eines Aktivierungsereignisses.
/// </summary>
public enum SpeechAssistantWakeWordEvidenceSource
{
    LexicalTranscript = 0,
    AcousticDetector = 1
}
