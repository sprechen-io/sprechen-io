namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

internal sealed class ThrowingFieldInteractionHandler : IFormVoiceCommandFieldInteractionHandler
{
    public Task<FormVoiceCommandFieldHandlingResult> HandleAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException("Field interaction must not run when the dialog pauses during the shared-variable announcement.");
}
