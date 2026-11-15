
namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt die Feldinteraktion eines Formularlaufs, damit der Sitzungsablauf sie über einen gemeinsamen Vertrag aufrufen kann.
/// </summary>
internal interface IFormVoiceCommandFieldInteractionHandler
{
    Task<FormVoiceCommandFieldHandlingResult> HandleAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        CancellationToken cancellationToken);
}


