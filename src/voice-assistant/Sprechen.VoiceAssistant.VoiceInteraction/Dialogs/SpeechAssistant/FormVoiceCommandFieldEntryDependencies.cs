namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Verfügbarkeitsprüfung, Gruppenansage und die Befehlsaufnahme bei
/// gesperrter Werteingabe für die beiden Feld-Einstiegszweige zusammen.
/// </summary>
internal sealed record FormVoiceCommandFieldEntryDependencies(
    FormVoiceCommandInputAvailabilityGuard InputAvailabilityGuard,
    FormVoiceCommandConfirmationTranscriptCapture ConfirmationTranscriptCapture,
    FormVoiceCommandReadOnlyFieldSpeaker ReadOnlyFieldSpeaker);
