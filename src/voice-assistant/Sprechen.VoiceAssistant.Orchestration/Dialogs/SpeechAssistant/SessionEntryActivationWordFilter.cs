using Sprechen.Speech.ModelRuntime.Services;
using Sprechen.Speech.TextNormalization;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Entfernt modellierte Aktivierungsformen aus Einstiegsantworten, damit deren Bestandteile nicht
/// unbeabsichtigt als Zustimmung zur Sprachfrage zählen. Ein zusätzlicher Antwortrest bleibt erhalten.
/// Eine reine Aktivierungsform liefert keinen verwendbaren Rest. Die Regel ist vom Ablauf des Fragezugs
/// unabhängig.
/// </summary>
internal static class SessionEntryActivationWordFilter
{
    /// <summary>
    /// Entfernt Aktivierungsformen unter Vergleich ohne diakritische Zeichen, erhält diese Zeichen
    /// jedoch im Antwortrest für die nachfolgende Antwortprüfung.
    /// </summary>
    internal static string? Strip(string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
        {
            return answer;
        }

        var accentedWords = TextNormalizer.NormalizeForAccentSensitiveMatching(answer)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (accentedWords.Length == 0)
        {
            return null;
        }

        // Positionen ohne diakritische Zeichen vergleichen und aus der akzenterhaltenden Wortfolge
        // entfernen. So vereinheitlicht die Aktivierungserkennung nicht zugleich die verbleibenden
        // Antwortwörter.
        var plainWords = accentedWords.Select(TextNormalizer.NormalizeForPhraseMatching).ToArray();
        var removed = new bool[accentedWords.Length];

        // Längere Aktivierungsformen zuerst entfernen, damit eine ebenfalls katalogisierte Teilform
        // keinen Rest der längeren Form zurücklässt.
        foreach (var form in EveryFormARecogniserCanReturn()
                     .Select(TextNormalizer.NormalizeForPhraseMatching)
                     .Where(form => form.Length > 0)
                     .Distinct(StringComparer.Ordinal)
                     .OrderByDescending(form => form.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length)
                     .ThenByDescending(form => form.Length))
        {
            var formWords = form.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var start = 0; start + formWords.Length <= plainWords.Length; start++)
            {
                var matches = true;
                for (var offset = 0; offset < formWords.Length; offset++)
                {
                    if (removed[start + offset]
                        || !string.Equals(plainWords[start + offset], formWords[offset], StringComparison.Ordinal))
                    {
                        matches = false;
                        break;
                    }
                }

                if (!matches)
                {
                    continue;
                }

                for (var offset = 0; offset < formWords.Length; offset++)
                {
                    removed[start + offset] = true;
                }
            }
        }

        var remainder = string.Join(
            ' ',
            accentedWords.Where((_, index) => !removed[index]));

        return remainder.Length == 0 ? null : remainder;
    }

    /// <summary>
    /// Vereinigt Aktivierungsformen und zusätzliche Erkennungsformen aus allen unterstützten Sprachen.
    /// Auch eine Form, die allein nicht aktivieren darf, kann in einem erkannten Antworttranskript
    /// stehen und muss deshalb beim Entfernen berücksichtigt werden.
    /// </summary>
    private static IEnumerable<string> EveryFormARecogniserCanReturn()
        => VoiceInterfaceModelCatalog.WakeWord.GetAllActivationForms()
            .Concat(VoiceInterfaceModelCatalog.SupportedLanguages
                .SelectMany(VoiceInterfaceModelCatalog.WakeWord.GetRecognitionOnlyActivationFormsForLanguage))
            .Distinct(StringComparer.OrdinalIgnoreCase);
}
