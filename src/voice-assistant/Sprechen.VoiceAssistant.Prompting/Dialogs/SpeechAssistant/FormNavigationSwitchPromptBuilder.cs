using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Baut die Rückfragen und Ergebnisansagen für Feldgruppensprünge und Formularwechsel. Fehlende
/// Voraussetzungen können mit ihren lesbaren Namen erläutert werden.
/// </summary>
public static class FormNavigationSwitchPromptBuilder
{
    internal static string GetJumpToMissingArgumentPrompt(string isoCode, PolitenessLevel politenessLevel)
        => FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIdVariantResolver.ResolvePolitenessVariantPromptId(
                FormDialogPromptIds.JumpToMissingArgument,
                politenessLevel));

    internal static string GetJumpToNotFoundPrompt(string isoCode)
        => FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.JumpToNotFound);

    internal static string GetSingleFormOnlyPrompt(string isoCode)
        => FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.SingleFormOnly);

    internal static string GetFormNotFoundPrompt(string isoCode)
        => FormPromptResolution.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.FormNotFound);

    internal static string GetAlreadyOnFormPrompt(string isoCode, string formName, PolitenessLevel politenessLevel)
        => FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIdVariantResolver.ResolvePolitenessVariantPromptId(
                FormDialogPromptIds.AlreadyOnForm,
                politenessLevel),
            CreateFormNameParameters(formName));

    internal static string GetSwitchFormConfirmationPrompt(string isoCode, string formName)
        => FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.SwitchFormConfirmation,
            CreateFormNameParameters(formName));

    internal static string GetPrerequisitesNotMetPrompt(
        string isoCode,
        string formName,
        IReadOnlyList<string> missingPrerequisites,
        PolitenessLevel politenessLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(formName);
        ArgumentNullException.ThrowIfNull(missingPrerequisites);

        var missingText = BuildMissingPrerequisitesText(missingPrerequisites);
        var promptId = missingText.Length == 0
            ? FormDialogPromptIdVariantResolver.ResolvePolitenessVariantPromptId(
                FormDialogPromptIds.PrerequisitesNotMetWithoutList,
                politenessLevel)
            : FormDialogPromptIdVariantResolver.ResolvePolitenessVariantPromptId(
                FormDialogPromptIds.PrerequisitesNotMetWithList,
                politenessLevel);

        return FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            promptId,
            CreatePrerequisiteParameters(formName, missingText));
    }

    public static string GetJumpToMissingArgumentPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.JumpToMissingArgument,
            isoCode,
            politenessLevel,
            GetJumpToMissingArgumentPrompt(isoCode, politenessLevel));
    }

    public static string GetJumpToNotFoundPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.JumpToNotFound,
            isoCode,
            politenessLevel,
            GetJumpToNotFoundPrompt(isoCode));
    }

    public static string GetSingleFormOnlyPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SingleFormOnly,
            isoCode,
            politenessLevel,
            GetSingleFormOnlyPrompt(isoCode));
    }

    public static string GetFormNotFoundPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.FormNotFound,
            isoCode,
            politenessLevel,
            GetFormNotFoundPrompt(isoCode));
    }

    public static string GetAlreadyOnFormPrompt(
        FormContract contract,
        string isoCode,
        string formName,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.AlreadyOnForm,
            isoCode,
            politenessLevel,
            GetAlreadyOnFormPrompt(isoCode, formName, politenessLevel),
            CreateFormNameParameters(formName));
    }

    public static string GetSwitchFormConfirmationPrompt(
        FormContract contract,
        string isoCode,
        string formName,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SwitchFormConfirmation,
            isoCode,
            politenessLevel,
            GetSwitchFormConfirmationPrompt(isoCode, formName),
            CreateFormNameParameters(formName));
    }

    public static string GetPrerequisitesNotMetPrompt(
        FormContract contract,
        string isoCode,
        string formName,
        IReadOnlyList<string> missingPrerequisites,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(formName);
        ArgumentNullException.ThrowIfNull(missingPrerequisites);

        var fallback = GetPrerequisitesNotMetPrompt(isoCode, formName, missingPrerequisites, politenessLevel);
        var missingText = BuildMissingPrerequisitesText(missingPrerequisites);
        var promptId = missingText.Length == 0
            ? FormDialogPromptIds.PrerequisitesNotMetWithoutList
            : FormDialogPromptIds.PrerequisitesNotMetWithList;

        return FormPromptResolution.ResolvePromptOrFallback(
            contract,
            promptId,
            isoCode,
            politenessLevel,
            fallback,
            CreatePrerequisiteParameters(formName, missingText));
    }

    private static string BuildMissingPrerequisitesText(IReadOnlyList<string> missingPrerequisites)
        => string.Join(", ", missingPrerequisites.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static Dictionary<string, string?> CreateFormNameParameters(string formName)
        => new()
        {
            [FormPromptTemplateParameterNames.FormName] = formName
        };

    private static Dictionary<string, string?> CreatePrerequisiteParameters(string formName, string missingText)
        => new()
        {
            [FormPromptTemplateParameterNames.FormName] = formName,
            [FormPromptTemplateParameterNames.MissingPrerequisites] = missingText
        };
}
