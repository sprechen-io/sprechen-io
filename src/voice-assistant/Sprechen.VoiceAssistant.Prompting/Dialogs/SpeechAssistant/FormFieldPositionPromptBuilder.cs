using System.Globalization;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>Verbindet die laufende Feldposition mit der Beschriftung und einem optionalen Hinweis
/// auf die erwartete Eingabe zur Einstiegsansage.</summary>
/// <remarks>Die Zusammensetzung bleibt auf Position, Label und den übergebenen kurzen Hinweis
/// begrenzt. Beispiele und Optionslisten werden hier nicht aus dem Feldmodell angehängt. Die
/// ausführliche Feldhilfe besitzt ihren eigenen Ausgabepfad. Die Herkunft und Länge des übergebenen
/// Hinweises bestimmt der Aufrufer.</remarks>
public static class FormFieldPositionPromptBuilder
{
    public static string GetFieldPositionPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        int index,
        int total,
        string fieldLabel,
        string? expectedInputHint)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var parameters = new Dictionary<string, string?>
        {
            [FormPromptTemplateParameterNames.Index] = index.ToString(CultureInfo.InvariantCulture),
            [FormPromptTemplateParameterNames.Total] = total.ToString(CultureInfo.InvariantCulture),
            [FormPromptTemplateParameterNames.FieldLabel] = fieldLabel
        };

        var fallback = FormPromptResolution.ResolveFallbackPrompt(
            isoCode,
            FormDialogPromptIds.FieldPosition,
            parameters);

        var positionLine = FormPromptResolution.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.FieldPosition,
            isoCode,
            politenessLevel,
            fallback,
            parameters);

        if (string.IsNullOrWhiteSpace(expectedInputHint))
        {
            return positionLine;
        }

        // Nur den übergebenen kurzen Eingabehinweis an die Positionszeile hängen. Die ausführliche
        // Beispiel- oder Optionshilfe wird hier nicht zusätzlich aufgebaut.
        return FormStatusPromptResolver.JoinPromptItems(new[] { positionLine, expectedInputHint.Trim() });
    }
}
