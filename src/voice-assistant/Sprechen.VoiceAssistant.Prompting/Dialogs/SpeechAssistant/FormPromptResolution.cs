using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt den Ansagefunktionen die gemeinsame Vorlagenauflösung bereit. Sie kann
/// zusätzlich melden, ob der ausgewählte Text aus einer Vertragsvorlage stammt.
/// </summary>
internal static class FormPromptResolution
{
    internal static string ResolveFallbackPrompt(string isoCode, string promptId)
        => FormPromptComposition.Default.ResolveFallbackPrompt(isoCode, promptId);

    internal static string ResolveFallbackPrompt(
        string isoCode,
        string promptId,
        IReadOnlyDictionary<string, string?> templateParameters)
        => FormPromptComposition.Default.ResolveFallbackPrompt(isoCode, promptId, templateParameters);

    internal static string ResolvePromptOrFallback(
        FormContract contract,
        string promptId,
        string isoCode,
        PolitenessLevel politenessLevel,
        string fallback)
        => FormPromptComposition.Default.ResolvePromptOrFallback(
            contract,
            promptId,
            isoCode,
            politenessLevel,
            fallback);

    internal static string ResolvePromptOrFallback(
        FormContract contract,
        string promptId,
        string isoCode,
        PolitenessLevel politenessLevel,
        string fallback,
        out bool resolvedFromContractOverride)
        => FormPromptComposition.Default.ResolvePromptOrFallback(
            contract,
            promptId,
            isoCode,
            politenessLevel,
            fallback,
            out resolvedFromContractOverride);

    internal static string ResolvePromptOrFallback(
        FormContract contract,
        string promptId,
        string isoCode,
        PolitenessLevel politenessLevel,
        string fallback,
        IReadOnlyDictionary<string, string?> templateParameters)
        => FormPromptComposition.Default.ResolvePromptOrFallback(
            contract,
            promptId,
            isoCode,
            politenessLevel,
            fallback,
            templateParameters);
}
