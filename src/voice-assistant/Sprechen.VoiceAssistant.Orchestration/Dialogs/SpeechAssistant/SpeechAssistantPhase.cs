namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet die übergeordneten Phasen des Assistenten, damit Ereignisse
/// der passenden Zustandsverarbeitung zugeordnet werden.
/// </summary>
public enum SpeechAssistantPhase
{
    Idle,
    Armed,
    Engaged,
    Paused,
    Completed
}


