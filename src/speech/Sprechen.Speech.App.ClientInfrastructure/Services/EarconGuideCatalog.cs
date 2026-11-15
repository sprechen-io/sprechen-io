namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Stellt die Einführung und Beschreibungen der vorgesehenen Hinweistöne
/// in der ausgewählten Sprache aus den modellierten Textressourcen zusammen.
/// </summary>
internal sealed class EarconGuideCatalog(SettingsVoiceLanguageResolver languageResolver)
{
    private readonly SettingsVoiceLanguageResolver _languageResolver =
        languageResolver ?? throw new ArgumentNullException(nameof(languageResolver));

    private static readonly (EarconId EarconId, string ResourceId)[] EarconGuideSteps =
    [
        (EarconId.FlowListening, VoiceInterfaceResourceIds.SessionSetupEarconFlowListening),
        (EarconId.FlowProcessing, VoiceInterfaceResourceIds.SessionSetupEarconFlowProcessing),
        (EarconId.ValidationSuccess, VoiceInterfaceResourceIds.SessionSetupEarconValidationSuccess),
        // Die Einführung soll nur Töne erklären, die im Anwendungsablauf tatsächlich vorkommen.
        // Eine vorhandene Textressource allein genügt dafür nicht. Töne für nicht mehr
        // hörbare Feld- oder Feldgruppenübergänge gehören deshalb nicht in diese Liste.
        (EarconId.VoiceInputMicOff, VoiceInterfaceResourceIds.SessionSetupEarconVoiceInputMicOff),
        (EarconId.VoiceInputMicOn, VoiceInterfaceResourceIds.SessionSetupEarconVoiceInputMicOn)
    ];

    internal EarconGuideState CreateState(string selectedIsoCode)
    {
        var items = EarconGuideSteps
            .Select(step => new EarconGuideItem(
                step.EarconId,
                _languageResolver.ResolveRequiredResourceText(step.ResourceId, selectedIsoCode)))
            .ToArray();

        return new EarconGuideState(
            selectedIsoCode,
            _languageResolver.ResolveRequiredResourceText(
                VoiceInterfaceResourceIds.SessionSetupEarconsIntro,
                selectedIsoCode),
            items);
    }
}
