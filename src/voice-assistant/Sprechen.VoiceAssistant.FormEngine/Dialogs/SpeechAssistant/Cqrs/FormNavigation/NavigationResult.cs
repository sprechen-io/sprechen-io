namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Verbindet das Navigationsergebnis mit den gegebenenfalls vorhandenen Feldkennungen
/// vor und nach dem Wechsel.
/// </summary>
/// <param name="Success">Ob die Navigation erfolgreich war.</param>
/// <param name="PreviousFieldId">Die gegebenenfalls vorhandene Feldkennung vor der Navigation.</param>
/// <param name="NewFieldId">Die gegebenenfalls vorhandene Feldkennung nach der Navigation.</param>
internal sealed record NavigationResult(bool Success, string? PreviousFieldId = null, string? NewFieldId = null);


