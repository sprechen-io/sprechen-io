namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Übergibt Ergebnisse der Aktivierungserkennung an den Ablauf des Sprachassistenten.
/// </summary>
public interface IWakeRuntimeOutcomeSink
{
    ValueTask PublishAsync(WakeRuntimeOutcome outcome, CancellationToken cancellationToken);
}
