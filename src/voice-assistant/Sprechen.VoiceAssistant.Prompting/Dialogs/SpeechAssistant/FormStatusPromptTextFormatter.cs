using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Übersetzt Zähler und Positionsangaben der Statusmomentaufnahme in lokalisierte Textteile.
/// Wird nur der Fortschritt bei Pflichtfeldern genannt, ergänzt er einen Hinweis auf vorhandene optionale Felder.
/// </summary>
internal static class FormStatusPromptTextFormatter
{
    public static string FormatProgressText(
        FormContract? contract,
        FormStatusSnapshot status,
        ProgressCalculationBasis basis,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        if (basis == ProgressCalculationBasis.RequiredFieldsOnly)
        {
            return FormatRequiredFieldsProgress(contract, status, isoCode, politenessLevel);
        }

        if (basis == ProgressCalculationBasis.FieldGroupBased)
        {
            return FormatFieldGroupProgress(contract, status, isoCode, politenessLevel);
        }

        return FormatAllFieldsProgress(contract, status, isoCode, politenessLevel);
    }

    public static string FormatFieldGroupText(
        FormContract? contract,
        FormStatusSnapshot status,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        // Ein fehlender Gruppentitel darf hier nicht durch die technische Gruppenkennung im
        // Sprechtext ersetzt werden. Bei vorhandener Gruppenanzahl wird die Position genannt,
        // andernfalls eine lokalisierte allgemeine Variante.
        var hasTitle = !string.IsNullOrWhiteSpace(status.CurrentFieldGroupTitle);
        var hasIndex = status.Navigation.TotalFieldGroups > 0;

        if (hasIndex)
        {
            return hasTitle
                ? FormStatusPromptResolver.ResolveOrFallback(
                    contract,
                    FormDialogPromptIds.StatusFieldGroupWithIndex,
                    isoCode,
                    politenessLevel,
                    new Dictionary<string, string?>
                    {
                        [FormPromptTemplateParameterNames.FieldGroupIndex] = status.Navigation.CurrentFieldGroupIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        [FormPromptTemplateParameterNames.Total] = status.Navigation.TotalFieldGroups.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        [FormPromptTemplateParameterNames.FieldGroupTitle] = status.CurrentFieldGroupTitle
                    })
                : FormStatusPromptResolver.ResolveOrFallback(
                    contract,
                    FormDialogPromptIds.StatusFieldGroupWithIndexNoTitle,
                    isoCode,
                    politenessLevel,
                    new Dictionary<string, string?>
                    {
                        [FormPromptTemplateParameterNames.FieldGroupIndex] = status.Navigation.CurrentFieldGroupIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        [FormPromptTemplateParameterNames.Total] = status.Navigation.TotalFieldGroups.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    });
        }

        return hasTitle
            ? FormStatusPromptResolver.ResolveOrFallback(
                contract,
                FormDialogPromptIds.StatusFieldGroupWithoutIndex,
                isoCode,
                politenessLevel,
                new Dictionary<string, string?>
                {
                    [FormPromptTemplateParameterNames.FieldGroupTitle] = status.CurrentFieldGroupTitle
                })
            : FormStatusPromptResolver.ResolveOrFallback(
                contract,
                FormDialogPromptIds.StatusFieldGroupNone,
                isoCode,
                politenessLevel);
    }

    public static string GetCurrentFieldText(
        FormContract? contract,
        FormStatusSnapshot status,
        string isoCode,
        PolitenessLevel politenessLevel)
        => string.IsNullOrWhiteSpace(status.CurrentFieldLabel)
            ? FormStatusPromptResolver.ResolveOrFallback(
                contract,
                FormDialogPromptIds.StatusCurrentFieldNone,
                isoCode,
                politenessLevel)
            : status.CurrentFieldLabel;

    public static string GetAddressingPrefix(
        FormContract? contract,
        string isoCode,
        PolitenessLevel politenessLevel)
        => FormStatusPromptResolver.ResolveOrFallback(
            contract,
            FormDialogPromptIds.StatusAddressingPrefix,
            FormStatusPromptResolver.ResolvePolitenessVariantPromptId(FormDialogPromptIds.StatusAddressingPrefix, politenessLevel),
            isoCode,
            politenessLevel);

    public static string FormatFormPosition(
        FormContract? contract,
        string isoCode,
        int currentFormIndex,
        int totalForms,
        PolitenessLevel politenessLevel)
        => FormStatusPromptResolver.ResolveOrFallback(
            contract,
            FormDialogPromptIds.StatusFormPosition,
            isoCode,
            politenessLevel,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.Current] = currentFormIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [FormPromptTemplateParameterNames.Total] = totalForms.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });

    private static string FormatRequiredFieldsProgress(
        FormContract? contract,
        FormStatusSnapshot status,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        var requiredProgress = status.Progress.RequiredFieldsOnly.Total > 0
            ? FormStatusPromptResolver.ResolveOrFallback(
                contract,
                FormDialogPromptIds.StatusProgressRequiredWithTotal,
                isoCode,
                politenessLevel,
                new Dictionary<string, string?>
                {
                    [FormPromptTemplateParameterNames.Current] = status.Progress.RequiredFieldsOnly.Completed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    [FormPromptTemplateParameterNames.Total] = status.Progress.RequiredFieldsOnly.Total.ToString(System.Globalization.CultureInfo.InvariantCulture)
                })
            : FormStatusPromptResolver.ResolveOrFallback(
                contract,
                FormDialogPromptIds.StatusProgressRequiredNone,
                isoCode,
                politenessLevel);

        // Die Feldposition zählt alle Felder, dieser Fortschritt nur Pflichtfelder. Ein
        // zusätzlicher Hinweis auf vorhandene optionale Felder erklärt die unterschiedlichen
        // Gesamtzahlen hörbar. Ohne positive Differenz bleibt es bei der Pflichtfeldansage.
        var optionalCount = status.Progress.AllFields.Total - status.Progress.RequiredFieldsOnly.Total;
        if (optionalCount <= 0)
        {
            return requiredProgress;
        }

        var optionalHint = optionalCount == 1
            ? FormStatusPromptResolver.ResolveOrFallback(
                contract,
                FormDialogPromptIds.StatusProgressOptionalOne,
                isoCode,
                politenessLevel)
            : FormStatusPromptResolver.ResolveOrFallback(
                contract,
                FormDialogPromptIds.StatusProgressOptionalMany,
                isoCode,
                politenessLevel,
                new Dictionary<string, string?>
                {
                    [FormPromptTemplateParameterNames.Count] = optionalCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });

        return $"{requiredProgress}. {optionalHint}";
    }

    private static string FormatFieldGroupProgress(
        FormContract? contract,
        FormStatusSnapshot status,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        if (status.Progress.FieldGroupBased.Total > 0)
        {
            return FormStatusPromptResolver.ResolveOrFallback(
                contract,
                FormDialogPromptIds.StatusProgressFieldGroupWithTotal,
                isoCode,
                politenessLevel,
                new Dictionary<string, string?>
                {
                    [FormPromptTemplateParameterNames.Current] = status.Progress.FieldGroupBased.Completed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    [FormPromptTemplateParameterNames.Total] = status.Progress.FieldGroupBased.Total.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        return FormStatusPromptResolver.ResolveOrFallback(
            contract,
            FormDialogPromptIds.StatusProgressFieldGroupNone,
            isoCode,
            politenessLevel);
    }

    private static string FormatAllFieldsProgress(
        FormContract? contract,
        FormStatusSnapshot status,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        if (status.Progress.AllFields.Total > 0)
        {
            return FormStatusPromptResolver.ResolveOrFallback(
                contract,
                FormDialogPromptIds.StatusProgressFieldWithTotal,
                isoCode,
                politenessLevel,
                new Dictionary<string, string?>
                {
                    [FormPromptTemplateParameterNames.Current] = status.Progress.AllFields.Completed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    [FormPromptTemplateParameterNames.Total] = status.Progress.AllFields.Total.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });
        }

        return FormStatusPromptResolver.ResolveOrFallback(
            contract,
            FormDialogPromptIds.StatusProgressFieldNone,
            isoCode,
            politenessLevel);
    }
}
