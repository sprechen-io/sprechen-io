using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Nimmt die Meldung einer überschrittenen Regeliterationsgrenze ohne weitere Wirkung
/// entgegen, wenn die Laufzeit ohne eigenen Rückmeldungskanal zusammengesetzt wurde.
/// </summary>
internal sealed class NullFormRuleLoopGuardFeedback : IFormRuleLoopGuardFeedback
{
    internal static readonly NullFormRuleLoopGuardFeedback Instance = new();

    private NullFormRuleLoopGuardFeedback()
    {
    }

    public void OnRuleLoopGuardBreach()
    {
    }
}
