using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Localization;

namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Löst Befehls- und Präfixäußerungen nach den zugehörigen Katalogregeln für Sprache und Höflichkeit auf.
/// </summary>
internal static class FormGlobalCommandUtteranceResolver
{
    internal static bool TryResolve(
        ResolvedGlobalCommandDefinition command,
        IReadOnlyDictionary<string, string> utterance,
        string isoCode,
        PolitenessLevel politenessLevel,
        out string resolved)
    {
        return LocalizedTextResolver.TryResolve(
            utterance,
            isoCode,
            command.DefaultLanguage,
            command.SupportedLanguages,
            command.StrictLocalization,
            politenessLevel,
            out resolved);
    }

    internal static bool TryResolve(
        ResolvedGlobalCommandPrefixDefinition prefix,
        IReadOnlyDictionary<string, string> utterance,
        string isoCode,
        PolitenessLevel politenessLevel,
        out string resolved)
    {
        return LocalizedTextResolver.TryResolve(
            utterance,
            isoCode,
            prefix.DefaultLanguage,
            prefix.SupportedLanguages,
            prefix.StrictLocalization,
            politenessLevel,
            out resolved);
    }
}
