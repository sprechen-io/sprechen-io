namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält eine mögliche gemeinsame Angabe zur Übernahme in das Zielformular zusammen.
/// </summary>
internal sealed record SharedVariableAdoptionCandidate(
    string VariableId,
    string Value,
    string SourceFormId);
