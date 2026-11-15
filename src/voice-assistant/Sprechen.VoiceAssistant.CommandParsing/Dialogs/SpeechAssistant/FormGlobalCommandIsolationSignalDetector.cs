using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, ob eine lokalisierte Äußerung einen aktivierten Formularbefehl isoliert ausdrückt.
/// </summary>
internal sealed class FormGlobalCommandIsolationSignalDetector
{
    public bool IsIsolatedCommandSignal(
        FormContract contract,
        string transcript,
        string commandId,
        string isoCode,
        PolitenessLevel politenessLevel,
        FormGlobalCommandParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(options);

        var normalizedTranscript = TextNormalizer.NormalizeForPhraseMatching(transcript);
        if (normalizedTranscript.Length == 0)
        {
            return false;
        }

        var normalizedCommandId = commandId.Trim();

        foreach (var command in FormVoiceCommandCatalogResolver.ResolveEffectiveCommands(contract))
        {
            if (!IsTargetCommand(command, normalizedCommandId))
            {
                continue;
            }

            if (HasIsolatedUtteranceSignal(command, normalizedTranscript, isoCode, politenessLevel, options))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTargetCommand(ResolvedGlobalCommandDefinition command, string commandId)
        => command.Enabled && string.Equals(command.CommandId, commandId, StringComparison.Ordinal);

    private static bool HasIsolatedUtteranceSignal(
        ResolvedGlobalCommandDefinition command,
        string normalizedTranscript,
        string isoCode,
        PolitenessLevel politenessLevel,
        FormGlobalCommandParserOptions options)
    {
        foreach (var catalogUtterance in command.Utterances)
        {
            if (!FormGlobalCommandUtteranceResolver.TryResolve(command, catalogUtterance, isoCode, politenessLevel, out var resolved)
                || string.IsNullOrWhiteSpace(resolved))
            {
                continue;
            }

            var normalizedUtterance = TextNormalizer.NormalizeForPhraseMatching(resolved);
            if (normalizedUtterance.Length == 0)
            {
                continue;
            }

            if (FormGlobalCommandIsolationMatcher.IsUtteranceWithOnlyPolitePadding(
                    normalizedTranscript,
                    normalizedUtterance,
                    options.PoliteModifierTokens))
            {
                return true;
            }
        }

        return false;
    }
}
