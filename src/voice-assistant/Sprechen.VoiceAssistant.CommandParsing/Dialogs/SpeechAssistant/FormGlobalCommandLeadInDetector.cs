namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Erkennt den gesprochenen Befehlsanker anhand der vom Aufrufer übergebenen gültigen Formen.
/// Die Formen stammen aus Katalog und Einstellungen. Eigene Ankerwörter werden nicht festgelegt.
/// Phrasenabgleich und kompakter Abgleich tolerieren abweichende Akzente, Großschreibung
/// und zusammengezogene Wörter. Der Anker muss dabei an einer Transkriptwortgrenze enden,
/// damit längere Wörter mit gleichem Anfang nicht als Befehlsanker gelten.
/// </summary>
internal sealed class FormGlobalCommandLeadInDetector
{
    /// <summary>
    /// Erkennt einen einleitenden Befehlsanker mit mindestens einem nachfolgenden Befehlswort.
    /// Die Ankerform muss an einer Wortgrenze enden, auch beim kompakten Abgleich.
    /// Ein alleinstehender Anker wird durch <see cref="IsBareCommandAnchor"/> erkannt.
    /// </summary>
    public bool HasCommandLeadIn(string transcript, IReadOnlyCollection<string> acceptedAnchorForms)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentNullException.ThrowIfNull(acceptedAnchorForms);

        var normalized = TextNormalizer.NormalizeForPhraseMatching(transcript);
        if (normalized.Length == 0)
        {
            return false;
        }

        var phraseTokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var anchorForm in acceptedAnchorForms)
        {
            var normalizedForm = TextNormalizer.NormalizeForPhraseMatching(anchorForm);
            if (normalizedForm.Length != 0
                && normalized.Length > normalizedForm.Length
                && normalized.StartsWith(normalizedForm, StringComparison.Ordinal)
                && normalized[normalizedForm.Length] == ' ')
            {
                return true;
            }

            var compactForm = TextNormalizer.NormalizeForCompactMatching(anchorForm);
            if (compactForm.Length != 0 && LeadingTokenRunEqualsCompactFormWithBody(phraseTokens, compactForm))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Erkennt einen alleinstehenden Befehlsanker ohne anschließenden Befehl. Dieser kennzeichnet
    /// einen unvollständigen Befehl, der im aktuellen Feld erneut erfragt werden muss, statt
    /// als Feldinhalt übernommen zu werden. Anders als <see cref="HasCommandLeadIn"/> prüft
    /// diese Methode die gesamte Äußerung auf Gleichheit, mit oder ohne Wortzwischenräume.
    /// </summary>
    public bool IsBareCommandAnchor(string transcript, IReadOnlyCollection<string> acceptedAnchorForms)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentNullException.ThrowIfNull(acceptedAnchorForms);

        var normalized = TextNormalizer.NormalizeForPhraseMatching(transcript);
        if (normalized.Length == 0)
        {
            return false;
        }

        var compactTranscript = TextNormalizer.NormalizeForCompactMatching(transcript);

        foreach (var anchorForm in acceptedAnchorForms)
        {
            var normalizedForm = TextNormalizer.NormalizeForPhraseMatching(anchorForm);
            if (normalizedForm.Length != 0 && string.Equals(normalized, normalizedForm, StringComparison.Ordinal))
            {
                return true;
            }

            var compactForm = TextNormalizer.NormalizeForCompactMatching(anchorForm);
            if (compactForm.Length != 0 && string.Equals(compactTranscript, compactForm, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Prüft, ob vollständige Anfangswörter zusammen genau die kompakte Ankerform ergeben
    /// und mindestens ein Befehlswort folgt. Die Längengrenze verhindert, dass lediglich
    /// der passende Anfang eines längeren Wortes als Anker akzeptiert wird.
    /// </summary>
    private static bool LeadingTokenRunEqualsCompactFormWithBody(string[] phraseTokens, string compactForm)
    {
        var accumulated = string.Empty;

        for (var index = 0; index < phraseTokens.Length; index++)
        {
            accumulated += TextNormalizer.NormalizeForCompactMatching(phraseTokens[index]);

            if (accumulated.Length > compactForm.Length)
            {
                return false;
            }

            if (accumulated.Length == compactForm.Length
                && string.Equals(accumulated, compactForm, StringComparison.Ordinal)
                && index < phraseTokens.Length - 1)
            {
                return true;
            }
        }

        return false;
    }
}
