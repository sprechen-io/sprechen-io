namespace Sprechen.Service.Abstractions;

/// <summary>
/// Konvertiert Erkennungssicherheit in ganzzahlige Prozentwerte von 0 bis 100.
/// Fehlende oder nicht endliche normalisierte Angaben werden als null zurückgegeben.
/// </summary>
public static class SttConfidencePercent
{
    public static int? FromNormalized(float? normalizedConfidence)
    {
        if (!normalizedConfidence.HasValue)
        {
            return null;
        }

        return FromNormalized((double)normalizedConfidence.Value);
    }

    public static int? FromNormalized(double? normalizedConfidence)
    {
        // Fehlende oder nicht endliche Angaben besitzen keinen auswertbaren Prozentwert.
        if (!normalizedConfidence.HasValue || !double.IsFinite(normalizedConfidence.Value))
        {
            return null;
        }

        return RecognitionConfidence.FromNormalized(normalizedConfidence.Value).AsPercent;
    }

    public static int? Clamp(int? confidencePercent)
    {
        if (!confidencePercent.HasValue)
        {
            return null;
        }

        return Math.Clamp(confidencePercent.Value, 0, 100);
    }
}
