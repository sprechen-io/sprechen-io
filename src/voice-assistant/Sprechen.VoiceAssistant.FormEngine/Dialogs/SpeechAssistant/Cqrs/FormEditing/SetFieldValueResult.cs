namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;

/// <summary>
/// Verbindet das Ergebnis einer Wertübernahme mit dem vorherigen und dem neuen Wert.
/// </summary>
/// <param name="Success">Ob die Operation erfolgreich war.</param>
/// <param name="PreviousValue">Der gegebenenfalls vorhandene vorherige Wert.</param>
/// <param name="NewValue">Der gegebenenfalls gesetzte neue Wert.</param>
internal sealed record SetFieldValueResult(bool Success, string? PreviousValue = null, string? NewValue = null);

