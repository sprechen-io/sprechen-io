namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Empfängt synchron die Meldung, dass die Regelausführung ihr Iterationsbudget erschöpft hat.
/// Der Zustand vor dem Ereignis ist dann bereits wiederhergestellt. Der Fehler muss
/// mit einem festen Laufzeitcode hörbar gemeldet und ohne Feldwerte in der Telemetrie
/// aufgezeichnet werden. Der Dialog wird vom wiederhergestellten Zustand aus fortgesetzt.
/// </summary>
public interface IFormRuleLoopGuardFeedback
{
    void OnRuleLoopGuardBreach();
}
