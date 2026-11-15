using Sprechen.Speech.App.Contracts.Annotations;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet die lokalen Befehlsaktionen während der blockweisen Zifferneingabe.
/// </summary>
[ActionKind]
public enum DigitsWorkflowCommand
{
    None,
    Continue,
    RemoveLastDigit,
    RemoveLastBlock,
    ReadbackDigits,
    RedictateLastBlock,
}

