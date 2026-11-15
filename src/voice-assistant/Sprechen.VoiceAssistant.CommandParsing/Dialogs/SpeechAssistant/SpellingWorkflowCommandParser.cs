using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Localization;

namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet lokalisierte Äußerungen beim Buchstabieren den modellierten Befehlskennungen zu.
/// </summary>
public static class SpellingWorkflowCommandParser
{
    public static bool TryParse(
        string transcript,
        string isoCode,
        PolitenessLevel politenessLevel,
        out string commandId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        commandId = string.Empty;

        // Beide Seiten gleichen auch ASCII-Umlautschreibweisen an: Die Phrasennormalisierung
        // macht aus „löschen“ bereits „loschen“, während „loeschen“ zusätzlich angeglichen werden muss.
        var normalizedTranscript = GermanUmlautTransliterator.CollapseAsciiTransliterations(
            TextNormalizer.NormalizeForPhraseMatching(transcript));
        if (normalizedTranscript.Length == 0)
        {
            return false;
        }

        foreach (var command in SpellingWorkflowCommandCatalogResolver.ResolveCommands())
        {
            if (!command.Enabled)
            {
                continue;
            }

            foreach (var catalogUtterance in command.Utterances)
            {
                if (!LocalizedTextResolver.TryResolve(
                        catalogUtterance,
                        isoCode,
                        command.DefaultLanguage,
                        command.SupportedLanguages,
                        command.StrictLocalization,
                        politenessLevel,
                        out var resolved)
                    || string.IsNullOrWhiteSpace(resolved))
                {
                    continue;
                }

                var normalizedUtterance = GermanUmlautTransliterator.CollapseAsciiTransliterations(
                    TextNormalizer.NormalizeForPhraseMatching(resolved));
                if (normalizedUtterance.Length == 0)
                {
                    continue;
                }

                if (FormGlobalCommandIsolationMatcher.IsUtteranceWithOnlyPolitePadding(
                        normalizedTranscript,
                        normalizedUtterance,
                        CommandPolitePaddingLexicon.UnionTokens))
                {
                    commandId = command.CommandId;
                    return true;
                }
            }
        }

        return false;
    }
}
