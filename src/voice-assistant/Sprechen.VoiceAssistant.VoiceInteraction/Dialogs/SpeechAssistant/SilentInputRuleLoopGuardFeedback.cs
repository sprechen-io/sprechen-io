namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Unterdrückt die Rückmeldung zur erkannten Regelschleife bei stiller Eingabe.
/// </summary>
internal sealed class SilentInputRuleLoopGuardFeedback : IFormRuleLoopGuardFeedback
{
    internal static readonly SilentInputRuleLoopGuardFeedback Instance = new();

    public void OnRuleLoopGuardBreach()
    {
    }
}
