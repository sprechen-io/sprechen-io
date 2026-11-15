using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert den lokalisierten Hinweis auf den freiwilligen Ziffernmodus, damit Zahlen bei Bedarf
/// ziffernweise eingegeben werden können. Die Vorlage nennt das modellierte Wechselwort. Dies ist
/// das Gegenstück zum Buchstabierangebot für Textfelder und zur schrittweisen Datumseingabe. Die
/// Funktion kündigt die Alternative an und wechselt den Modus nicht selbst.
/// </summary>
public static class FormDigitsModeDiscoveryPromptBuilder
{
    private static readonly FormPromptComposition PromptComposition = FormPromptComposition.Default;

    public static string GetDigitsModeAvailablePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.DigitsModeAvailable,
            isoCode,
            politenessLevel,
            PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.DigitsModeAvailable));
    }
}
