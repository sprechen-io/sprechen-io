namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält den für Bestätigungsentscheidungen benötigten Phasen-, Kandidaten- und
/// Zählerstand fest. So lassen sich die Bestätigungsregeln ohne Zugriff auf den
/// veränderlichen Laufzustand auswerten.
/// </summary>
internal sealed record SelectionConfirmationStateSnapshot<TSelection>(
    SelectionDialogPhase Phase,
    SelectionDialogTurnSnapshot<TSelection> Turn,
    SelectionConfirmationCaptureStage? Stage,
    int CandidateCaptureCount,
    int ConfirmationRepairFailureCount,
    int RecoverableFailureCount,
    SelectionDialogPrompt? ActiveStagePrompt)
    where TSelection : class;
