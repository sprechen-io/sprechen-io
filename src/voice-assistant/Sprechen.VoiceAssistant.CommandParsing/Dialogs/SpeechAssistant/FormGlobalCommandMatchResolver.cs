using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Wählt den bestbewerteten aktivierten Formularbefehl samt optionalem Argument aus der Äußerung.
/// </summary>
internal sealed class FormGlobalCommandMatchResolver
{
    public bool TryParse(
        FormContract contract,
        string transcript,
        string isoCode,
        PolitenessLevel politenessLevel,
        FormGlobalCommandParserOptions options,
        out string commandId,
        out string? argument)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(options);

        commandId = string.Empty;
        argument = null;

        var normalized = TextNormalizer.NormalizeForPhraseMatching(transcript);
        if (normalized.Length == 0)
        {
            return false;
        }

        var candidatesByCommand = BuildCandidatesByCommand(contract, normalized, isoCode, politenessLevel, options);
        if (candidatesByCommand.Count == 0)
        {
            return false;
        }

        var bestCandidate = candidatesByCommand.Values
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.MatchedUtteranceTokenCount)
            .ThenBy(candidate => candidate.CommandId, StringComparer.Ordinal)
            .First();

        commandId = bestCandidate.CommandId;
        argument = bestCandidate.Argument;
        return true;
    }

    private static Dictionary<string, GlobalCommandCandidate> BuildCandidatesByCommand(
        FormContract contract,
        string normalizedTranscript,
        string isoCode,
        PolitenessLevel politenessLevel,
        FormGlobalCommandParserOptions options)
    {
        var candidatesByCommand = new Dictionary<string, GlobalCommandCandidate>(StringComparer.Ordinal);
        foreach (var command in FormVoiceCommandCatalogResolver.ResolveEffectiveCommands(contract))
        {
            if (!command.Enabled)
            {
                continue;
            }

            AddCommandCandidates(command, normalizedTranscript, isoCode, politenessLevel, options, candidatesByCommand);
        }

        return candidatesByCommand;
    }

    private static void AddCommandCandidates(
        ResolvedGlobalCommandDefinition command,
        string normalizedTranscript,
        string isoCode,
        PolitenessLevel politenessLevel,
        FormGlobalCommandParserOptions options,
        Dictionary<string, GlobalCommandCandidate> candidatesByCommand)
    {
        foreach (var catalogUtterance in command.Utterances)
        {
            if (!TryNormalizeUtterance(command, catalogUtterance, isoCode, politenessLevel, out var utteranceNormalized))
            {
                continue;
            }

            if (FormGlobalCommandCandidateMatcher.TryMatchWithOptionalArgument(
                    command.CommandId,
                    normalizedTranscript,
                    utteranceNormalized,
                    options,
                    out var candidateCommandId,
                    out var candidateArgument,
                    out var candidateScore))
            {
                var utteranceTokenCount = utteranceNormalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                ConsiderMatch(candidatesByCommand, candidateCommandId, candidateArgument, candidateScore, utteranceTokenCount);
            }
        }
    }

    private static bool TryNormalizeUtterance(
        ResolvedGlobalCommandDefinition command,
        LocalizedText catalogUtterance,
        string isoCode,
        PolitenessLevel politenessLevel,
        out string utteranceNormalized)
    {
        utteranceNormalized = string.Empty;
        if (!FormGlobalCommandUtteranceResolver.TryResolve(command, catalogUtterance, isoCode, politenessLevel, out var resolved)
            || string.IsNullOrWhiteSpace(resolved))
        {
            return false;
        }

        utteranceNormalized = TextNormalizer.NormalizeForPhraseMatching(resolved);
        return utteranceNormalized.Length > 0;
    }

    private static void ConsiderMatch(
        Dictionary<string, GlobalCommandCandidate> candidatesByCommand,
        string candidateCommandId,
        string? candidateArgument,
        double candidateScore,
        int matchedUtteranceTokenCount)
    {
        if (!candidatesByCommand.TryGetValue(candidateCommandId, out var currentCandidate)
            || candidateScore > currentCandidate.Score
            || (candidateScore == currentCandidate.Score
                && matchedUtteranceTokenCount > currentCandidate.MatchedUtteranceTokenCount))
        {
            candidatesByCommand[candidateCommandId] = new GlobalCommandCandidate(
                candidateCommandId,
                candidateArgument,
                candidateScore,
                matchedUtteranceTokenCount);
        }
    }
}
