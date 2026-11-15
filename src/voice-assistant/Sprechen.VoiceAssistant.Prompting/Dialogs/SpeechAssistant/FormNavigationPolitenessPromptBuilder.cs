using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Formatiert Rückfragen und Rückmeldungen zum Wechsel der Anrede. Verfügbarkeit, zulässige
/// Optionen und ausgewählte Anrede werden vom aufrufenden Ablauf vorgegeben.
/// </summary>
public static class FormNavigationPolitenessPromptBuilder
{
    internal static string GetUnavailablePrompt(string isoCode)
        => FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.ChangePolitenessUnavailable);

    internal static string GetMissingArgumentPrompt(string isoCode, string supportedPolitenessOptions)
        => FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.ChangePolitenessMissingArgument,
            CreateItemsParameters(supportedPolitenessOptions));

    internal static string GetUnsupportedPrompt(string isoCode, string supportedPolitenessOptions)
        => FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.ChangePolitenessUnsupported,
            CreateItemsParameters(supportedPolitenessOptions));

    internal static string GetAlreadySelectedPrompt(string isoCode, string politenessLabel)
        => FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.ChangePolitenessAlreadySelected,
            CreatePolitenessLabelParameters(politenessLabel));

    internal static string GetConfirmationPrompt(string isoCode, string politenessLabel)
        => FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.ChangePolitenessConfirmation,
            CreatePolitenessLabelParameters(politenessLabel));

    public static string GetUnavailablePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.ChangePolitenessUnavailable,
            isoCode,
            politenessLevel,
            GetUnavailablePrompt(isoCode));
    }

    public static string GetMissingArgumentPrompt(
        FormContract contract,
        string isoCode,
        string supportedPolitenessOptions,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.ChangePolitenessMissingArgument,
            isoCode,
            politenessLevel,
            GetMissingArgumentPrompt(isoCode, supportedPolitenessOptions),
            CreateItemsParameters(supportedPolitenessOptions));
    }

    public static string GetUnsupportedPrompt(
        FormContract contract,
        string isoCode,
        string supportedPolitenessOptions,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.ChangePolitenessUnsupported,
            isoCode,
            politenessLevel,
            GetUnsupportedPrompt(isoCode, supportedPolitenessOptions),
            CreateItemsParameters(supportedPolitenessOptions));
    }

    public static string GetAlreadySelectedPrompt(
        FormContract contract,
        string isoCode,
        string politenessLabel,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.ChangePolitenessAlreadySelected,
            isoCode,
            politenessLevel,
            GetAlreadySelectedPrompt(isoCode, politenessLabel),
            CreatePolitenessLabelParameters(politenessLabel));
    }

    public static string GetConfirmationPrompt(
        FormContract contract,
        string isoCode,
        string politenessLabel,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.ChangePolitenessConfirmation,
            isoCode,
            politenessLevel,
            GetConfirmationPrompt(isoCode, politenessLabel),
            CreatePolitenessLabelParameters(politenessLabel));
    }

    private static Dictionary<string, string?> CreateItemsParameters(string supportedPolitenessOptions)
        => new()
        {
            [FormPromptTemplateParameterNames.Items] = supportedPolitenessOptions
        };

    private static Dictionary<string, string?> CreatePolitenessLabelParameters(string politenessLabel)
        => new()
        {
            [FormPromptTemplateParameterNames.PolitenessLabel] = politenessLabel
        };
}
