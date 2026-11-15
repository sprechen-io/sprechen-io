namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt eine Ablehnung vor der Erfassung mit Diagnosecode, Ansagetext und
/// ursprünglich gewünschtem Modus.
/// </summary>
internal sealed record FormVoiceCommandPreCaptureDenial(
    string IssueCode,
    string Prompt,
    string ExpectedInputMode);
