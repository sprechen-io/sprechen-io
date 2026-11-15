namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Verknüpft das Ergebnis einer stillen Feldtransaktion mit ihrer Auftragskennung, damit der
/// Reducer nur den noch passenden laufenden Auftrag abschließt.
/// </summary>
internal sealed record SpeechAssistantSilentInputTransactionCompletedEvent(
    string OperationId,
    FormSilentInputTransactionResult Result) : SpeechAssistantEvent;
