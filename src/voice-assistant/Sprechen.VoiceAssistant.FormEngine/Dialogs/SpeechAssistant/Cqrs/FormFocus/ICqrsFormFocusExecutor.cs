using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormFocus;

/// <summary>
/// Stellt Fokusabfragen und gezielte Änderungen von Fokus und Eingabemodus für den Formularablauf bereit.
/// </summary>
internal interface ICqrsFormFocusExecutor
{
    Task<FormFieldSnapshot> GetFocusedFieldAsync();

    Task<bool> SetFocusToFieldIdAsync(string fieldId);

    Task<bool> SetFocusedFieldInputModeAsync(string inputMode);

    Task<FormFieldInputModeAvailability> GetFocusedFieldInputModeAvailabilityAsync(string inputMode);

    Task RecordDeniedFieldTurnAnnouncementAsync();

    Task<bool> HasDeniedFieldTurnAnnouncementAsync();
}
