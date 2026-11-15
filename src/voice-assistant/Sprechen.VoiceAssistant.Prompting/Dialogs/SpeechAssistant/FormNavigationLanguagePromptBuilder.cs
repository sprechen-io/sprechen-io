using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Formatiert Rückfragen und Rückmeldungen zum Sprachwechsel anhand der verfügbaren Sprachen und
/// der benannten Auswahl, ohne die Sitzungssprache selbst zu ändern.
/// </summary>
public static class FormNavigationLanguagePromptBuilder
{
    internal static string GetMissingArgumentPrompt(string isoCode, string supportedLanguages)
        => FormDialogFlowPromptTextResolver.GetMissingArgumentPrompt(isoCode, supportedLanguages);

    internal static string GetUnsupportedPrompt(string isoCode, string supportedLanguages)
        => FormDialogFlowPromptTextResolver.GetUnsupportedPrompt(isoCode, supportedLanguages);

    internal static string GetAlreadySelectedPrompt(string isoCode, string languageName)
        => FormDialogFlowPromptTextResolver.GetAlreadySelectedPrompt(isoCode, languageName);

    internal static string GetConfirmationPrompt(string isoCode, string languageName)
        => FormDialogFlowPromptTextResolver.GetConfirmationPrompt(isoCode, languageName);

    public static string GetMissingArgumentPrompt(
        FormContract contract,
        string isoCode,
        string supportedLanguages,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.ChangeLanguageMissingArgument,
            isoCode,
            politenessLevel,
            GetMissingArgumentPrompt(isoCode, supportedLanguages),
            CreateItemsParameters(supportedLanguages));
    }

    public static string GetUnsupportedPrompt(
        FormContract contract,
        string isoCode,
        string supportedLanguages,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.ChangeLanguageUnsupported,
            isoCode,
            politenessLevel,
            GetUnsupportedPrompt(isoCode, supportedLanguages),
            CreateItemsParameters(supportedLanguages));
    }

    public static string GetAlreadySelectedPrompt(
        FormContract contract,
        string isoCode,
        string languageName,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.ChangeLanguageAlreadySelected,
            isoCode,
            politenessLevel,
            GetAlreadySelectedPrompt(isoCode, languageName),
            CreateLanguageNameParameters(languageName));
    }

    public static string GetConfirmationPrompt(
        FormContract contract,
        string isoCode,
        string languageName,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.ChangeLanguageConfirmation,
            isoCode,
            politenessLevel,
            GetConfirmationPrompt(isoCode, languageName),
            CreateLanguageNameParameters(languageName));
    }

    private static Dictionary<string, string?> CreateItemsParameters(string supportedLanguages)
        => new()
        {
            [FormPromptTemplateParameterNames.Items] = supportedLanguages
        };

    private static Dictionary<string, string?> CreateLanguageNameParameters(string languageName)
        => new()
        {
            [FormPromptTemplateParameterNames.LanguageName] = languageName
        };
}
