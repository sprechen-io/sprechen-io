namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet Eignungsabfrage und Anwendung stiller Eingaben als Zugang zur feldbezogenen Bearbeitung ohne Sprachaufnahme.
/// </summary>
public interface IFormDialogSilentInput : IFormDialogSilentInputEligibility
{
    Task<FormSilentInputResult> ApplySilentInputAsync(
        FormSilentInputRequest request,
        CancellationToken cancellationToken);
}
