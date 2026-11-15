using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Formuliert die Zusammenfassung aus bereits projizierten Feldwerten. Lesbarer, maskierter,
/// ausdrücklich leerer, nicht verfügbarer und lediglich gespeicherter Inhalt erhalten die dafür
/// vorgesehenen Ansageformen.
/// </summary>
public static class FormSummaryPromptBuilder
{
    private static readonly FormPromptComposition PromptComposition = FormPromptComposition.Default;

    public static string GetEmptySummaryPrompt(string isoCode)
        => PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.EmptySummary);

    public static string GetSummaryValueLinePrompt(string isoCode, string fieldLabel, string fieldValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldValue);

        return PromptComposition.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.SummaryValueLine,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.FieldLabel] = fieldLabel.Trim(),
                [FormPromptTemplateParameterNames.FieldValue] = fieldValue.Trim()
            });
    }

    public static string GetSummaryStoredValueLinePrompt(string isoCode, string fieldLabel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);

        return PromptComposition.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.SummaryStoredValue,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.FieldLabel] = fieldLabel.Trim()
            });
    }

    public static string GetNoErrorsPrompt(string isoCode)
        => PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.NoErrors);

    public static string BuildSummaryPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        IReadOnlyList<FormSummaryItem> items)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            return GetEmptySummaryPrompt(contract, isoCode, politenessLevel);
        }

        var lines = items
            .Select(item => ComposeSummaryLine(contract, isoCode, politenessLevel, item))
            .ToArray();

        return string.Join(". ", lines);
    }

    public static string GetEmptySummaryPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.EmptySummary,
            isoCode,
            politenessLevel,
            GetEmptySummaryPrompt(isoCode));
    }

    public static string GetSummaryValueLinePrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        string fieldValue,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldValue);

        var normalizedFieldLabel = fieldLabel.Trim();
        var normalizedFieldValue = fieldValue.Trim();

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SummaryValueLine,
            isoCode,
            politenessLevel,
            GetSummaryValueLinePrompt(isoCode, normalizedFieldLabel, normalizedFieldValue),
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.FieldLabel] = normalizedFieldLabel,
                [FormPromptTemplateParameterNames.FieldValue] = normalizedFieldValue
            });
    }

    public static string GetSummaryStoredValueLinePrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);

        var normalizedFieldLabel = fieldLabel.Trim();
        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SummaryStoredValue,
            isoCode,
            politenessLevel,
            GetSummaryStoredValueLinePrompt(isoCode, normalizedFieldLabel),
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.FieldLabel] = normalizedFieldLabel
            });
    }

    public static string GetNoErrorsPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.NoErrors,
            isoCode,
            politenessLevel,
            GetNoErrorsPrompt(isoCode));
    }

    public static string GetSummaryUnavailableLinePrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);

        var unavailable = FormConfirmationPromptBuilder.GetSpokenValueUnavailablePrompt(
            contract,
            isoCode,
            politenessLevel);
        return GetSummaryValueLinePrompt(contract, isoCode, fieldLabel.Trim(), unavailable, politenessLevel);
    }

    private static string ComposeSummaryLine(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        FormSummaryItem item)
        => item.Status switch
        {
            FormSpokenValueProjectionStatus.Unavailable => GetSummaryUnavailableLinePrompt(
                contract,
                isoCode,
                item.FieldLabel,
                politenessLevel),
            FormSpokenValueProjectionStatus.FullValue
                or FormSpokenValueProjectionStatus.Empty
                or FormSpokenValueProjectionStatus.Masked
                when !string.IsNullOrWhiteSpace(item.SpokenValue)
                => GetSummaryValueLinePrompt(
                    contract,
                    isoCode,
                    item.FieldLabel,
                    item.SpokenValue,
                    politenessLevel),
            _ => GetSummaryStoredValueLinePrompt(contract, isoCode, item.FieldLabel, politenessLevel)
        };
}
