namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Erkennt Befehlsphrasen mit angehängtem Argument und entfernt abschließende Höflichkeitswörter.
/// </summary>
internal static class FormGlobalCommandArgumentMatcher
{
    public static bool TryMatch(
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

        if (normalizedTranscript == normalizedUtterance)
        {
            commandId = commandIdCandidate;
            score = options.ExactMatchScore;
            return true;
        }

        if (!normalizedTranscript.StartsWith(normalizedUtterance + " ", StringComparison.Ordinal))
        {
            return false;
        }

        argument = TrimArgumentNoise(normalizedTranscript[normalizedUtterance.Length..].Trim(), options);
        if (argument.Length == 0)
        {
            argument = null;
            return false;
        }

        commandId = commandIdCandidate;
        score = options.ArgumentMatchScore;
        return true;
    }

    private static string TrimArgumentNoise(string argument, FormGlobalCommandParserOptions options)
    {
        if (argument.Length == 0)
        {
            return string.Empty;
        }

        var tokens = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        while (tokens.Count > 0 && options.PoliteModifierTokens.Contains(tokens[^1]))
        {
            tokens.RemoveAt(tokens.Count - 1);
        }

        return string.Join(' ', tokens);
    }
}
