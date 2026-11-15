namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Unterscheidet fehlende Sprache, fehlende führende Aktivierung und erkannten Aufruf, damit nur aktivierte Äußerungen einen Befehl freigeben.
/// </summary>
public static class WakeWordActivationRule
{
    public static WakeWordActivationEvaluation EvaluateLeadingWakeWord(
        string text,
        int? confidencePercent,
        WakeWordInventory wakeWordInventory)
    {
        ArgumentNullException.ThrowIfNull(wakeWordInventory);

        var transcriptText = text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(transcriptText))
        {
            return new WakeWordActivationEvaluation(
                WakeWordActivationStatus.NoRecognizedSpeech,
                string.Empty,
                string.Empty,
                confidencePercent);
        }

        if (!new WakeWordCommandTextExtractor().TryExtractLeadingCommandText(transcriptText, wakeWordInventory, out var commandText))
        {
            return new WakeWordActivationEvaluation(
                WakeWordActivationStatus.MissingLeadingWakeWord,
                transcriptText,
                string.Empty,
                confidencePercent);
        }

        return new WakeWordActivationEvaluation(
            WakeWordActivationStatus.LeadingWakeWordDetected,
            transcriptText,
            commandText,
            confidencePercent);
    }
}
