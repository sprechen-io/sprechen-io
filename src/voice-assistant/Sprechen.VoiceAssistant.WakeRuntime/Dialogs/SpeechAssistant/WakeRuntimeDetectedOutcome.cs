namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Gemeinsamer Ergebnistyp erfolgreicher Aktivierungserkennungen mit Korrelationskennung.
/// </summary>
public abstract record WakeRuntimeDetectedOutcome : WakeRuntimeOutcome
{
    protected WakeRuntimeDetectedOutcome(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        CorrelationId = correlationId.Trim();
    }

    public string CorrelationId { get; }
}
