using Sprechen.Speech.App.Contracts.Annotations;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet Abschluss, Übernahme und erneutes Vorlesen einer gesammelten
/// Mengenauswahl, damit Auflösung und Ausführung dieselben Aktionen verwenden.
/// </summary>
[ActionKind]
public enum SetWorkflowCommand
{
    None,
    Complete,
    AcceptSelection,
    ReadbackSelection,
}
