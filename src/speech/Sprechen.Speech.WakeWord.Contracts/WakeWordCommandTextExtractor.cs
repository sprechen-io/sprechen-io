namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Trennt den Befehl hinter aufeinanderfolgenden Aktivierungsformen ab. Anfangsprüfung und freie Suche teilen sich denselben Tokenvergleich.
/// </summary>
public sealed class WakeWordCommandTextExtractor
{
    public bool ContainsWakeWord(string transcript, IReadOnlyList<string> wakeWordActivationForms)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentNullException.ThrowIfNull(wakeWordActivationForms);

        return TryExtractCommandText(
            transcript,
            new WakeWordInventory(
                wakeWordActivationForms,
                []),
            out _);
    }

    public bool ContainsLeadingWakeWord(string transcript, IReadOnlyList<string> wakeWordActivationForms)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentNullException.ThrowIfNull(wakeWordActivationForms);

        return TryExtractLeadingCommandText(
            transcript,
            new WakeWordInventory(
                wakeWordActivationForms,
                []),
            out _);
    }

    public bool ContainsWakeWord(string transcript, WakeWordInventory wakeWordInventory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentNullException.ThrowIfNull(wakeWordInventory);

        return TryExtractCommandText(transcript, wakeWordInventory, out _);
    }

    public bool ContainsLeadingWakeWord(string transcript, WakeWordInventory wakeWordInventory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentNullException.ThrowIfNull(wakeWordInventory);

        return TryExtractLeadingCommandText(transcript, wakeWordInventory, out _);
    }

    public bool TryExtractCommandText(
        string transcript,
        IReadOnlyList<string> wakeWordActivationForms,
        out string commandText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentNullException.ThrowIfNull(wakeWordActivationForms);

        return TryExtractCommandText(
            transcript,
            new WakeWordInventory(
                wakeWordActivationForms,
                []),
            out commandText);
    }

    public bool TryExtractCommandText(
        string transcript,
        WakeWordInventory wakeWordInventory,
        out string commandText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentNullException.ThrowIfNull(wakeWordInventory);

        commandText = string.Empty;
        var transcriptTokens = TokenizeTranscript(transcript);
        var activationCandidates = WakeWordActivationCandidateCatalog.Build(wakeWordInventory);
        if (transcriptTokens.Length == 0 || activationCandidates.Count == 0)
        {
            return false;
        }

        for (var startIndex = 0; startIndex < transcriptTokens.Length; startIndex++)
        {
            var activationSequenceEnd = FindActivationSequenceEnd(
                transcriptTokens,
                startIndex,
                activationCandidates);
            if (activationSequenceEnd <= startIndex)
            {
                continue;
            }

            var commandTokens = transcriptTokens[activationSequenceEnd..];
            commandText = string.Join(' ', commandTokens);
            return true;
        }

        return false;
    }

    public bool TryExtractLeadingCommandText(
        string transcript,
        IReadOnlyList<string> wakeWordActivationForms,
        out string commandText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentNullException.ThrowIfNull(wakeWordActivationForms);

        return TryExtractLeadingCommandText(
            transcript,
            new WakeWordInventory(
                wakeWordActivationForms,
                []),
            out commandText);
    }

    public bool TryExtractLeadingCommandText(
        string transcript,
        WakeWordInventory wakeWordInventory,
        out string commandText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentNullException.ThrowIfNull(wakeWordInventory);

        commandText = string.Empty;
        var transcriptTokens = TokenizeTranscript(transcript);
        var activationCandidates = WakeWordActivationCandidateCatalog.Build(wakeWordInventory);
        if (transcriptTokens.Length == 0 || activationCandidates.Count == 0)
        {
            return false;
        }

        var activationSequenceEnd = FindActivationSequenceEnd(
            transcriptTokens,
            startIndex: 0,
            activationCandidates);
        if (activationSequenceEnd <= 0)
        {
            return false;
        }

        var commandTokens = transcriptTokens[activationSequenceEnd..];
        commandText = string.Join(' ', commandTokens);
        return true;
    }

    private static int FindActivationSequenceEnd(
        string[] transcriptTokens,
        int startIndex,
        IReadOnlyList<string[]> activationCandidates)
    {
        var endIndex = startIndex;
        while (endIndex < transcriptTokens.Length)
        {
            var activationLength = WakeWordTokenSequenceMatcher.FindLengthAt(
                transcriptTokens,
                endIndex,
                activationCandidates);
            if (activationLength <= 0)
            {
                break;
            }

            endIndex += activationLength;
        }

        return endIndex;
    }

    private static string[] TokenizeTranscript(string transcript)
    {
        var normalizedTranscript = TextNormalizer.NormalizeForTokenComparison(transcript);
        return TextNormalizer.TokenizeLettersOrDigits(normalizedTranscript).ToArray();
    }
}
