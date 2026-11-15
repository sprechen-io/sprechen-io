namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Berechnet den kleinsten gemeldeten Vertrauenswert der Beiträge, die noch
/// im zusammengesetzten Wert enthalten sind. Beiträge ohne Vertrauenswert werden
/// übersprungen. Fehlt er bei allen Beiträgen, bleibt das Ergebnis unbekannt
/// (<c>null</c>). Die Wertepuffer verwenden dafür dieselbe Regel.
/// </summary>
internal sealed class AccumulatedValueConfidence
{
    public int? SurvivingMinimumPercent(IEnumerable<int?> contributionConfidencePercents)
    {
        ArgumentNullException.ThrowIfNull(contributionConfidencePercents);

        int? minimum = null;
        foreach (var confidence in contributionConfidencePercents)
        {
            if (!confidence.HasValue)
            {
                continue;
            }

            minimum = minimum.HasValue
                ? Math.Min(minimum.Value, confidence.Value)
                : confidence.Value;
        }

        return minimum;
    }
}
