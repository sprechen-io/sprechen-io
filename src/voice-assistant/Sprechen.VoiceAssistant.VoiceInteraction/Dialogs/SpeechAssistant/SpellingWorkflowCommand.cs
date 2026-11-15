using Sprechen.Speech.App.Contracts.Annotations;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet Abschluss-, Bearbeitungs- und Vorlesebefehle während der Buchstabiereingabe.
/// </summary>
[ActionKind]
public enum SpellingWorkflowCommand
{
    None,
    Complete,
    RemoveLastCharacter,
    RemoveLastBlock,
    ClearBuffer,
    ReadbackCharacters,
}
