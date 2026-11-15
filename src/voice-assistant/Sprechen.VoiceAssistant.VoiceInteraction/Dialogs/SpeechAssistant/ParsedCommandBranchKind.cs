namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet die möglichen Interpretationszweige für die anschließende Befehlsbehandlung.
/// </summary>
internal enum ParsedCommandBranchKind
{
    None,
    PromptedFailure,
    ManualCorrection,
    Execution
}
