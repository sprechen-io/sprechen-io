namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Führt eine stille Formulareingabe mit Sitzungskontext als zusammenhängenden Laufzeitvorgang aus.
/// </summary>
public interface IFormSilentInputTransaction
{
    Task<FormSilentInputTransactionResult> ExecuteAsync(
        FormSilentInputTransactionRequest request,
        CancellationToken cancellationToken);
}
