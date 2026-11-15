using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Formatiert Rückmeldungen zum aktuellen Ziffernpuffer und zu dessen Bearbeitung. Leerer Puffer,
/// Vorlesen, Entfernen und Fortsetzen erhalten passende lokalisierte Ansagen.
/// </summary>
public static class FormDigitsPromptBuilder
{
    private static readonly FormPromptComposition PromptComposition = FormPromptComposition.Default;

    public static string GetDigitsWorkflowPrompt(string isoCode, string digits)
    {
        ArgumentNullException.ThrowIfNull(digits);
        var normalizedDigits = digits.Trim();

        if (normalizedDigits.Length == 0)
        {
            return PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.DigitsWorkflowEmpty);
        }

        return PromptComposition.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.DigitsWorkflowCaptured,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Digits] = normalizedDigits
            });
    }

    public static string GetDigitsNoValuePrompt(string isoCode)
        => PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.DigitsNoValue);

    public static string GetDigitsLastDigitRemovedPrompt(string isoCode, string digits)
    {
        ArgumentNullException.ThrowIfNull(digits);
        var normalizedDigits = digits.Trim();

        if (normalizedDigits.Length == 0)
        {
            return GetDigitsNoValuePrompt(isoCode);
        }

        return PromptComposition.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.DigitsLastDigitRemoved,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Digits] = normalizedDigits
            });
    }

    public static string GetDigitsLastBlockRemovedPrompt(string isoCode, string digits)
    {
        ArgumentNullException.ThrowIfNull(digits);
        var normalizedDigits = digits.Trim();

        if (normalizedDigits.Length == 0)
        {
            return GetDigitsNoValuePrompt(isoCode);
        }

        return PromptComposition.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.DigitsLastBlockRemoved,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Digits] = normalizedDigits
            });
    }

    public static string GetDigitsCurrentReadbackPrompt(string isoCode, string digits)
    {
        ArgumentNullException.ThrowIfNull(digits);
        var normalizedDigits = digits.Trim();

        if (normalizedDigits.Length == 0)
        {
            return GetDigitsNoValuePrompt(isoCode);
        }

        return PromptComposition.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.DigitsCurrentReadback,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Digits] = normalizedDigits
            });
    }

    public static string GetDigitsRedictateBlockPrompt(string isoCode)
        => PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.DigitsRedictateBlock);

    public static string GetDigitsContinueOrDonePrompt(string isoCode)
        => PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.DigitsContinueOrDone);

    public static string GetDigitsWorkflowPrompt(
        FormContract contract,
        string isoCode,
        string digits,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(digits);

        var fallback = GetDigitsWorkflowPrompt(isoCode, digits);
        var normalizedDigits = digits.Trim();
        var promptId = normalizedDigits.Length == 0
            ? FormDialogPromptIds.DigitsWorkflowEmpty
            : FormDialogPromptIds.DigitsWorkflowCaptured;

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            promptId,
            isoCode,
            politenessLevel,
            fallback,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Digits] = normalizedDigits
            });
    }

    public static string GetDigitsNoValuePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.DigitsNoValue,
            isoCode,
            politenessLevel,
            GetDigitsNoValuePrompt(isoCode));
    }

    public static string GetDigitsLastDigitRemovedPrompt(
        FormContract contract,
        string isoCode,
        string digits,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(digits);

        var normalizedDigits = digits.Trim();
        if (normalizedDigits.Length == 0)
        {
            return GetDigitsNoValuePrompt(contract, isoCode, politenessLevel);
        }

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.DigitsLastDigitRemoved,
            isoCode,
            politenessLevel,
            GetDigitsLastDigitRemovedPrompt(isoCode, digits),
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Digits] = normalizedDigits
            });
    }

    public static string GetDigitsLastBlockRemovedPrompt(
        FormContract contract,
        string isoCode,
        string digits,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(digits);

        var normalizedDigits = digits.Trim();
        if (normalizedDigits.Length == 0)
        {
            return GetDigitsNoValuePrompt(contract, isoCode, politenessLevel);
        }

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.DigitsLastBlockRemoved,
            isoCode,
            politenessLevel,
            GetDigitsLastBlockRemovedPrompt(isoCode, digits),
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Digits] = normalizedDigits
            });
    }

    public static string GetDigitsCurrentReadbackPrompt(
        FormContract contract,
        string isoCode,
        string digits,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(digits);

        var normalizedDigits = digits.Trim();
        if (normalizedDigits.Length == 0)
        {
            return GetDigitsNoValuePrompt(contract, isoCode, politenessLevel);
        }

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.DigitsCurrentReadback,
            isoCode,
            politenessLevel,
            GetDigitsCurrentReadbackPrompt(isoCode, digits),
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Digits] = normalizedDigits
            });
    }

    public static string GetDigitsRedictateBlockPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.DigitsRedictateBlock,
            isoCode,
            politenessLevel,
            GetDigitsRedictateBlockPrompt(isoCode));
    }

    public static string GetDigitsContinueOrDonePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.DigitsContinueOrDone,
            isoCode,
            politenessLevel,
            GetDigitsContinueOrDonePrompt(isoCode));
    }
}
