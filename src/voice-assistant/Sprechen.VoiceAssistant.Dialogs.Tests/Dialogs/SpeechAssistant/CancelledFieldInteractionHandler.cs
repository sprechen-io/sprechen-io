using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

internal sealed class CancelledFieldInteractionHandler : IFormVoiceCommandFieldInteractionHandler
{
    public Task<FormVoiceCommandFieldHandlingResult> HandleAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        CancellationToken cancellationToken)
        => Task.FromResult(
            FormVoiceCommandFieldHandlingResult.FromDialogResult(
                VoiceCommandDialogResult.Cancelled(
                    turnContext.Context.Contract.ApplicationId,
                    turnContext.Context.Contract.FormId,
                    ImmutableDictionary<string, string>.Empty)));
}
