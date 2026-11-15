namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt den Feldkontext und die Dienste zum schrittweisen Sammeln von Zeichen
/// oder Auswahlwerten. Derselbe Kontext wird an die jeweiligen Eingabestrategien
/// weitergegeben. <see cref="Interpreter"/> ermöglicht Befehlsausführung und
/// die anschließende Prüfung von Fokus- oder Moduswechseln.
/// <see cref="AnchoredCommands"/> verarbeitet vorangestellte Aktivierungswörter
/// über die gemeinsame Befehlssteuerung.
/// </summary>
internal sealed record CharacterAccumulationContext(
    FormVoiceCommandContext Context,
    IFormDialogRuntime Interpreter,
    FormFieldSnapshot Field,
    EvaluationSession Session,
    FormVoiceCommandStateTracker StateTracker,
    FormVoiceCommandAutoHelpTracker AutoHelp,
    FormVoiceCommandUnrecognizedInputEscalationPolicy Escalation,
    CharacterAccumulationAnchoredCommandFlow AnchoredCommands);
