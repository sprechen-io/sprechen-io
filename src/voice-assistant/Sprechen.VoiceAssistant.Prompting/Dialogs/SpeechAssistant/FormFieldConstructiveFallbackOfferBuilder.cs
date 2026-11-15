using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Wählt anhand der effektiven Interaktionsart ein alternatives Eingabeangebot:
/// schrittweise Datumseingabe, Buchstabieren für Text und Code oder Ziffernmodus für Zahlen.
/// Dafür werden dieselben Vorlagen wie bei der Einführung verwendet. Auswahl- und Zeitfelder
/// erhalten hier kein zusätzliches Angebot. Ihre Options- beziehungsweise Beispielhilfe
/// wird getrennt ausgegeben. Andere Interaktionsarten liefern ebenfalls <c>null</c>.
/// Wiederholungsschwelle und Reihenfolge der Hilfen steuert der Aufrufer.
/// </summary>
public static class FormFieldConstructiveFallbackOfferBuilder
{
    /// <summary>
    /// Liefert das lokalisierte alternative Eingabeangebot für den effektiven Feldtyp oder
    /// <c>null</c>, wenn hier kein eigener Modus angeboten wird.
    /// </summary>
    public static string? TryBuildOffer(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        FormFieldSnapshot field)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(field);

        var effectiveInteractionType =
            FieldInteractionSemantics.ResolveEffectiveInteractionType(field.CodeStyle, field.ValueTypeKind);

        return effectiveInteractionType switch
        {
            EffectiveFieldInteractionTypes.Date =>
                FormDateComponentPromptBuilder.GetDateComponentEntryAvailablePrompt(contract, isoCode, politenessLevel),
            EffectiveFieldInteractionTypes.Text or EffectiveFieldInteractionTypes.Code =>
                FormSpellingOverrideDiscoveryPromptBuilder.GetSpellingOverrideAvailablePrompt(contract, isoCode, politenessLevel),
            EffectiveFieldInteractionTypes.Number =>
                FormDigitsModeDiscoveryPromptBuilder.GetDigitsModeAvailablePrompt(contract, isoCode, politenessLevel),
            _ => null,
        };
    }
}
