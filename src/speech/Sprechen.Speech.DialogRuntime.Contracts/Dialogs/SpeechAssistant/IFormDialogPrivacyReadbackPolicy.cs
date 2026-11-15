namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Sprachfreigaben und bereitet datenschutzgerechte Bestätigungen von Feldwerten vor.
/// </summary>
public interface IFormDialogPrivacyReadbackPolicy
{
    Task<VoiceInputAllowedResult> IsVoiceInputAllowedAsync(string variableId);

    Task<VoiceInputAllowedResult> ShouldOfferSilentInputOptionAsync(string variableId);

    Task<bool> RequiresReadbackOptInAsync(string variableId);

    Task<bool> CanSpeakValueInConfirmationAsync(string variableId, bool explicitOptIn);

    Task<bool> IsSpellOutAllowedInConfirmationAsync(string variableId);

    Task<string> BuildMaskedReadbackAsync(string variableId, string value, string isoCode);
}
