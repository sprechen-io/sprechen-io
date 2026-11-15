using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Baut die Optionshilfe für einen Wiederholungszug bei geschlossener Einfach- oder
/// Mehrfachauswahl. Sie nennt die feldbezogenen Optionsnamen und den modellierten Hinweis zur
/// Auswahl per Nummer, statt ein möglicherweise falsch erkanntes Transkript zurückzulesen. Der
/// gemeinsame Resolver für die effektive Interaktionsart begrenzt die Funktion auf Auswahlfelder.
/// Fehlen verwendbare Optionsnamen, bleibt die weitere Fehlerbehandlung beim Aufrufer.
/// </summary>
public static class FormConstrainedOptionRecoveryPromptBuilder
{
    /// <summary>
    /// Formatiert die Optionshilfe nur für eine geschlossene Auswahl mit benennbaren Optionen. Der
    /// Rückgabewert zeigt an, ob eine nichtleere Ansage entstanden ist.
    /// </summary>
    public static bool TryBuildPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        FormFieldSnapshot field,
        out string prompt)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(field);

        prompt = string.Empty;

        var effectiveInteractionType =
            FieldInteractionSemantics.ResolveEffectiveInteractionType(field.CodeStyle, field.ValueTypeKind);
        if (effectiveInteractionType is not (EffectiveFieldInteractionTypes.SingleSelect
            or EffectiveFieldInteractionTypes.MultiSelect))
        {
            return false;
        }

        var optionNames = FormatOptionNames(field.Options);
        if (optionNames.Length == 0)
        {
            return false;
        }

        var parameters = CreateItemsParameters(optionNames);
        prompt = FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.UnrecognizedInputConstrainedOptions,
            isoCode,
            politenessLevel,
            FormPromptResolution.ResolveFallbackPrompt(
                isoCode,
                FormDialogPromptIds.UnrecognizedInputConstrainedOptions,
                parameters),
            parameters);
        return !string.IsNullOrWhiteSpace(prompt);
    }

    // Die verwendbaren Optionsnamen behalten ihre Deklarationsreihenfolge. Leere Beschriftungen
    // entfallen aus dem Sprechtext. Hier werden weder Ersatznamen noch neue Optionsnummern erzeugt.
    private static string FormatOptionNames(ImmutableArray<FormChoiceOption> options)
    {
        var names = options
            .Select(option => option.DisplayName?.Trim())
            .Where(name => !string.IsNullOrEmpty(name));
        return string.Join(", ", names);
    }

    private static Dictionary<string, string?> CreateItemsParameters(string items)
        => new()
        {
            [FormPromptTemplateParameterNames.Items] = items
        };
}
