namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

internal sealed record FormVoiceCommandConfirmationTurnCancelExecutionFixture(
    FormVoiceCommandGlobalCommandExecutionService Service,
    FormVoiceCommandContext Context,
    IFormDialogRuntime Interpreter,
    EvaluationSession Session,
    FormVoiceCommandStateTracker StateTracker);
