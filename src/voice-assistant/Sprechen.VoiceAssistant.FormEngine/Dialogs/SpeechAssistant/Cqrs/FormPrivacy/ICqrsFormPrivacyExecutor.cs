using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;

/// <summary>
/// Bietet Datenschutzentscheidungen für Spracheingabe und Vorlesen sowie die Aufbereitung vorzulesender Werte an.
/// </summary>
internal interface ICqrsFormPrivacyExecutor
{
    Task<VoiceInputAllowedResult> IsVoiceInputAllowedAsync(string variableId);

    Task<VoiceInputAllowedResult> ShouldOfferSilentInputOptionAsync(string variableId);

    Task<bool> RequiresReadbackOptInAsync(string variableId);

    Task<bool> CanSpeakValueInConfirmationAsync(string variableId, bool explicitOptIn);

    Task<bool> IsSpellOutAllowedInConfirmationAsync(string variableId);

    Task<string> BuildMaskedReadbackAsync(string variableId, string value, string isoCode);
}
