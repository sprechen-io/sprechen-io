namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Wählt den passenden Phrasenabgleich für globale Befehle mit oder ohne Argument.
/// </summary>
internal static class FormGlobalCommandCandidateMatcher
{
    internal static bool TryMatchWithOptionalArgument(
        string commandIdCandidate,
        string normalizedTranscript,
        string normalizedUtterance,
        FormGlobalCommandParserOptions options,
        out string commandId,
        out string? argument,
        out double score)
    {
        commandId = string.Empty;
        argument = null;
        score = 0d;

        if (RequiresArgumentAwarePrefixMatch(commandIdCandidate))
        {
            return FormGlobalCommandArgumentMatcher.TryMatch(
                commandIdCandidate,
                normalizedTranscript,
                normalizedUtterance,
                options,
                out commandId,
                out argument,
                out score);
        }

        if (!FormGlobalCommandPhraseMatchScorer.TryScore(
                normalizedTranscript,
                normalizedUtterance,
                options,
                out score))
        {
            return false;
        }

        commandId = commandIdCandidate;
        return true;
    }

    private static bool RequiresArgumentAwarePrefixMatch(string commandIdCandidate)
        => commandIdCandidate is GlobalCommandIds.JumpTo
            or GlobalCommandIds.SwitchForm
            or GlobalCommandIds.ChangeLanguage
            or GlobalCommandIds.ChangePoliteness;
}
