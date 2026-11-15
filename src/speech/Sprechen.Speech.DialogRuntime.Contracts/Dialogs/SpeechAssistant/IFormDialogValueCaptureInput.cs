namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Übernimmt Transkripte oder bereits zusammengesetzte Werte in die Formularlaufzeit
/// und ermöglicht Fokus- und Eingabemoduswechsel. Zulässigkeitsabfragen liegen im
/// getrennten Vertrag <see cref="IFormDialogInputModePolicyInput"/>.
/// </summary>
public interface IFormDialogValueCaptureInput
{
    Task<FormInputResult> TryApplyTranscriptAsync(string transcript);

    /// <summary>
    /// Übernimmt einen bereits zusammengesetzten Wert, etwa das Ergebnis einer
    /// Buchstabierung, und prüft ihn. Die Buchstaben- oder Zifferngrammatik wird nicht
    /// erneut angewendet, damit Großschreibung und Wörter erhalten bleiben, die
    /// mit Einträgen der Buchstabiertabelle übereinstimmen.
    /// </summary>
    Task<FormInputResult> TryApplyComposedValueAsync(string composedValue);

    Task<bool> TrySetFocusToFieldIdAsync(string fieldId);

    Task<bool> TrySetFocusedFieldInputModeAsync(string inputMode);
}
