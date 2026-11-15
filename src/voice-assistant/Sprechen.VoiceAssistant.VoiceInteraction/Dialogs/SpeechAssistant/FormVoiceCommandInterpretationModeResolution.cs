namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet den aufgelösten Interpretationsmodus mit der Befehlsignalklasse für die anschließende Verarbeitung des Transkripts.
/// </summary>
internal sealed record FormVoiceCommandInterpretationModeResolution(
    FormVoiceCommandInterpretationMode Mode,
    string CommandSignalClass);
