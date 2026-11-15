namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Erfasst die höchste mittlere Absolutamplitude und die Anzahl verarbeiteter Audioblöcke,
/// damit fehlende Daten von digitaler Stille unterschieden werden können.
/// </summary>
/// <remarks>
/// Bei vorhandenen Frames kennzeichnet eine Höchstamplitude von null digitale Stille.
/// Eine Framezahl von null bedeutet, dass kein Audio verarbeitet wurde. Werte unter
/// der Sprachschwelle belegen allein nicht, warum kein Sprachbeginn erkannt wurde.
/// </remarks>
internal sealed class PcmSpeechCapturePeakLevel
{
    internal int Observed { get; private set; }

    internal int ObservedFrames { get; private set; }

    /// <summary>Zuletzt geschätzter Hintergrundpegel. 0 bei Verwendung der statischen Schwelle.</summary>
    internal int EstimatedFloor { get; private set; }

    /// <summary>Zuletzt verwendete Sprachbeginnschwelle. 0 vor dem ersten Vergleich.</summary>
    internal int AppliedOnsetThreshold { get; private set; }

    internal void Observe(int averageAmplitude)
    {
        ObservedFrames++;
        if (averageAmplitude > Observed)
        {
            Observed = averageAmplitude;
        }
    }

    internal int ObserveOnsetDecision(int estimatedFloor, int appliedOnsetThreshold)
    {
        EstimatedFloor = estimatedFloor;
        AppliedOnsetThreshold = appliedOnsetThreshold;
        return appliedOnsetThreshold;
    }

    internal int ResolveOnsetThreshold(
        PcmSpeechCaptureStartContextPolicy startContextPolicy,
        PcmSpeechCaptureBufferedAudioState bufferedAudioState,
        int staticThreshold)
    {
        if (!startContextPolicy.UsesAdaptiveSpeechFloor() || !bufferedAudioState.HasAdaptiveSpeechFloorSamples)
        {
            return ObserveOnsetDecision(0, staticThreshold);
        }

        var estimatedFloor = bufferedAudioState.ResolveAdaptiveSpeechFloor();
        return ObserveOnsetDecision(
            estimatedFloor,
            startContextPolicy.ResolveAdaptiveOnsetThreshold(estimatedFloor));
    }
}
