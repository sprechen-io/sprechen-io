using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt für Status- und Übersichtsansagen die gemeinsame Auflösung
/// von Sprache, Anredevarianten und Vorlagen bereit.
/// </summary>
internal static class FormStatusPromptResolver
{
    public static string JoinPromptItems(IEnumerable<string> items)
        => FormPromptComposition.Default.JoinPromptItems(items);

    public static string NormalizeIsoCode(string isoCode)
        => FormPromptComposition.Default.NormalizeIsoCode(isoCode);

    public static string ResolveOrFallback(
        FormContract? contract,
        string promptId,
        string fallbackPromptId,
        string isoCode,
        PolitenessLevel politenessLevel)
        => FormPromptComposition.Default.ResolveOrFallback(
            contract,
            promptId,
            fallbackPromptId,
            isoCode,
            politenessLevel);

    public static string ResolveOrFallback(
        FormContract? contract,
        string promptId,
        string fallbackPromptId,
        string isoCode,
        PolitenessLevel politenessLevel,
        IReadOnlyDictionary<string, string?> templateParameters)
        => FormPromptComposition.Default.ResolveOrFallback(
            contract,
            promptId,
            fallbackPromptId,
            isoCode,
            politenessLevel,
            templateParameters);

    public static string ResolveOrFallback(
        FormContract? contract,
        string promptId,
        string isoCode,
        PolitenessLevel politenessLevel)
        => FormPromptComposition.Default.ResolveOrFallback(contract, promptId, isoCode, politenessLevel);

    public static string ResolveOrFallback(
        FormContract? contract,
        string promptId,
        string isoCode,
        PolitenessLevel politenessLevel,
        IReadOnlyDictionary<string, string?> templateParameters)
        => FormPromptComposition.Default.ResolveOrFallback(contract, promptId, isoCode, politenessLevel, templateParameters);

    public static string ResolvePolitenessVariantPromptId(string promptId, PolitenessLevel politenessLevel)
        => FormPromptComposition.Default.ResolvePolitenessVariantPromptId(promptId, politenessLevel);
}
