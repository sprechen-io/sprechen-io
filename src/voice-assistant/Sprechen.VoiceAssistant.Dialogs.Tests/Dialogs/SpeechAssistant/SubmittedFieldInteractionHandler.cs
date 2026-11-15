namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

internal sealed class SubmittedFieldInteractionHandler : IFormVoiceCommandFieldInteractionHandler
{
    public Task<FormVoiceCommandFieldHandlingResult> HandleAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        CancellationToken cancellationToken)
        => Task.FromResult(FormVoiceCommandFieldHandlingResult.Submitted());
}
