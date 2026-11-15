using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Baut die lokalisierten Ansagen für die mehrzügige Mehrfachauswahl. Die laufende Bestätigung
/// verbindet Feldlabel, bisherige Auswahl und die Möglichkeiten Übernehmen, Ändern oder weitere
/// Optionen nennen in einem Zug. Das Übernahmewort macht den Abschluss im angebotenen Ablauf
/// bekannt. Die Verarbeitung weiterer Abschlusswörter gehört dem Eingabeablauf. Der Builder liefert
/// daneben Texte für noch fehlende Auswahl, ausdrücklich leere Auswahl und die Einführung in diesen
/// Eingabeweg.
/// </summary>
public static class FormSetWorkflowPromptBuilder
{
    private static readonly FormPromptComposition PromptComposition = FormPromptComposition.Default;

    /// <summary>
    /// Verbindet Feldlabel und bisherigen Auswahltext mit der dreifachen Handlungsaufforderung zum
    /// Übernehmen, Ändern oder Ergänzen. Die Anredevariante wird über den gemeinsamen
    /// Schlüsselzusatz aufgelöst.
    /// </summary>
    public static string GetSetSelectionConfirmPrompt(
        FormContract contract,
        string isoCode,
        string fieldLabel,
        string selectedItems,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldLabel);
        ArgumentNullException.ThrowIfNull(selectedItems);

        var parameters = CreateSelectionConfirmParameters(fieldLabel, selectedItems);
        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SetSelectionConfirm,
            isoCode,
            politenessLevel,
            PromptComposition.ResolveFallbackPrompt(
                isoCode,
                FormDialogPromptIdVariantResolver.ResolvePolitenessVariantPromptId(
                    FormDialogPromptIds.SetSelectionConfirm,
                    politenessLevel),
                parameters),
            parameters);
    }

    /// <summary>
    /// Meldet, dass noch keine Option gewählt ist, und nennt ein erstes Beispiel.
    /// Die Vorlage nennt kein Abschlusswort. Ob eine ausdrücklich leere Auswahl zulässig ist,
    /// entscheiden die Regeln für das Feld.
    /// </summary>
    public static string GetSetNoneSelectedYetPrompt(
        FormContract contract,
        string isoCode,
        string firstOptionDisplayName,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(firstOptionDisplayName);

        var parameters = new Dictionary<string, string?>
        {
            [FormPromptTemplateParameterNames.FirstOption] = firstOptionDisplayName.Trim()
        };

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SetNoneSelectedYet,
            isoCode,
            politenessLevel,
            PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.SetNoneSelectedYet, parameters),
            parameters);
    }

    /// <summary>
    /// Liefert den lokalisierten Text für eine ausdrücklich leere Mehrfachauswahl.
    /// Bestätigung und Zusammenfassung können damit eine zulässige leere Auswahl benennen,
    /// statt eine leere Optionsliste auszugeben.
    /// </summary>
    public static string GetSetEmptySelectionValue(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SetEmptySelectionValue,
            isoCode,
            politenessLevel,
            PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.SetEmptySelectionValue));
    }

    /// <summary>
    /// Liefert die Einführung in das Sammeln und anschließende Übernehmen von Optionen mit der
    /// passenden Anredevariante. Den einmaligen Aufruf am ersten Mehrfachauswahlfeld steuert der
    /// Sitzungsmerker des Aufrufers.
    /// </summary>
    public static string GetSetAccumulationStructurePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SetAccumulationStructure,
            isoCode,
            politenessLevel,
            PromptComposition.ResolveFallbackPrompt(
                isoCode,
                FormDialogPromptIdVariantResolver.ResolvePolitenessVariantPromptId(
                    FormDialogPromptIds.SetAccumulationStructure,
                    politenessLevel)));
    }

    public static string GetSetSelectionHintPrompt(
        FormContract contract,
        string isoCode,
        int minSelected,
        int maxSelected,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var parameters = new Dictionary<string, string?>
        {
            [FormPromptTemplateParameterNames.Min] = minSelected.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [FormPromptTemplateParameterNames.Max] = maxSelected.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SetSelectionHint,
            isoCode,
            politenessLevel,
            PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.SetSelectionHint, parameters),
            parameters);
    }

    private static Dictionary<string, string?> CreateSelectionConfirmParameters(string fieldLabel, string selectedItems)
        => new()
        {
            [FormPromptTemplateParameterNames.FieldLabel] = fieldLabel.Trim(),
            [FormPromptTemplateParameterNames.Items] = selectedItems.Trim()
        };
}
