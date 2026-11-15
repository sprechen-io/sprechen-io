using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert den lokalisierten Hinweis auf den freiwilligen Buchstabiermodus für Text- und
/// Codefelder. Die Vorlage benennt das modellierte Wechselwort, statt eine Methode vorzuschreiben.
/// Der Aufrufer steuert die Einführung mit seinem Sitzungsmerker. Derselbe Text kann auch als
/// alternatives Eingabeangebot bei Problemen aufgelöst werden.
/// </summary>
public static class FormSpellingOverrideDiscoveryPromptBuilder
{
    private static readonly FormPromptComposition PromptComposition = FormPromptComposition.Default;

    public static string GetSpellingOverrideAvailablePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.SpellingOverrideAvailable,
            isoCode,
            politenessLevel,
            PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.SpellingOverrideAvailable));
    }
}
