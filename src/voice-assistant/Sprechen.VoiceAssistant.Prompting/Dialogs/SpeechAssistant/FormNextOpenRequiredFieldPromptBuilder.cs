using System.Globalization;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Übersetzt die Suche nach dem nächsten offenen Pflichtfeld in eine Ansage: Wechsel,
/// Rücksprung zum Anfang, bereits einziges Ziel oder kein offenes Pflichtfeld.
/// Die verbleibende Anzahl stammt aus dem Ergebnis. Die Feldbezeichnung wird separat übergeben.
/// </summary>
public static class FormNextOpenRequiredFieldPromptBuilder
{
    public static string BuildPrompt(
        FormContract? contract,
        FocusNextOpenRequiredFieldResult result,
        string? fieldLabel,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(result);

        var promptId = result.Kind switch
        {
            FocusNextOpenRequiredFieldResultKind.Moved => FormDialogPromptIds.NextOpenRequiredFieldMoved,
            FocusNextOpenRequiredFieldResultKind.WrappedToStart => FormDialogPromptIds.NextOpenRequiredFieldWrapped,
            FocusNextOpenRequiredFieldResultKind.AlreadyOnSoleTarget => FormDialogPromptIds.NextOpenRequiredFieldSoleTarget,
            _ => FormDialogPromptIds.NextOpenRequiredFieldNone,
        };

        var templateParameters = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [FormPromptTemplateParameterNames.FieldLabel] = fieldLabel,
            [FormPromptTemplateParameterNames.OpenRequiredCount] =
                result.RemainingOpenRequiredCount.ToString(CultureInfo.InvariantCulture),
        };

        return FormStatusPromptResolver.ResolveOrFallback(contract, promptId, isoCode, politenessLevel, templateParameters);
    }
}
