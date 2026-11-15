namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ermittelt die stille Eingabemöglichkeit des fokussierten Felds für die Veröffentlichung passender Bedienaktionen.
/// </summary>
public interface IFormDialogSilentInputEligibility
{
    Task<FormSilentInputEligibilityProjection?> GetFocusedSilentInputEligibilityAsync();
}
