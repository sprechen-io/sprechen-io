namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Erzeugt die Laufzeit eines Formulardialogs, damit Aufrufer den konkreten Interpreter nicht zusammensetzen müssen.
/// </summary>
public interface IFormDialogRuntimeFactory
{
    IFormDialogRuntime Create(
        IFormDialogRuntimeContext context,
        string sessionId,
        IFormRuleLoopGuardFeedback loopGuardFeedback);
}
