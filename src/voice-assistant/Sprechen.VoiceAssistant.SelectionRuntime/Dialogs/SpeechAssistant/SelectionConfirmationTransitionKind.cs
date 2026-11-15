namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet mögliche Übergänge nach einer Bestätigungsentscheidung, damit
/// Zustandsänderung und Rückmeldung den vorgesehenen Ablauf umsetzen können.
/// </summary>
internal enum SelectionConfirmationTransitionKind
{
    PassThroughSelection,
    EnterInitialConfirmation,
    Launch,
    EnterExplicitAuthorization,
    EnterConfidenceFaultRetry,
    PreserveStage,
    Reselect,
    Cancellation,
    VerificationFailedTerminal,
    RepairExhaustedTerminal
}
