using Sprechen.Speech.TextNormalization;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft den Abbruch am Sitzungseinstieg getrennt von der Sprachannahme. Nur die vollständig
/// normalisierte Äußerung darf einer vorgegebenen Abbruchform entsprechen, damit ein eingebettetes Wort
/// nicht allein den Einstieg beendet.
/// </summary>
internal static class SessionEntryCancelReader
{
    /// <summary>
    /// Vergleicht das gesamte normalisierte Transkript mit den vorgegebenen Abbruchäußerungen.
    /// </summary>
    public static bool IsCancelled(string? transcript, IReadOnlyList<string> cancelUtterances)
    {
        ArgumentNullException.ThrowIfNull(cancelUtterances);

        if (string.IsNullOrWhiteSpace(transcript))
        {
            return false;
        }

        var normalized = TextNormalizer.NormalizeForPhraseMatching(transcript);
        if (normalized.Length == 0)
        {
            return false;
        }

        foreach (var utterance in cancelUtterances)
        {
            var candidate = TextNormalizer.NormalizeForPhraseMatching(utterance);
            if (candidate.Length > 0 && string.Equals(normalized, candidate, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
