namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Bestimmt Schutzzeiten und Sprachbeginnschwellen anhand der Wiedergabesituation,
/// damit die Aufnahme den Nachlauf eigener Sprachausgabe berücksichtigen kann.
/// </summary>
internal sealed class PcmSpeechCaptureStartContextPolicy
{
    private readonly SpeechCaptureStartContext _captureStartContext;
    private readonly PcmSpeechCaptureSegmenterOptions _options;
    private readonly double _recentPlaybackOutputLatencyMs;

    public PcmSpeechCaptureStartContextPolicy(
        SpeechCaptureStartContext captureStartContext,
        PcmSpeechCaptureSegmenterOptions options,
        double recentPlaybackOutputLatencyMs = 0)
    {
        _captureStartContext = captureStartContext;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _recentPlaybackOutputLatencyMs = double.IsFinite(recentPlaybackOutputLatencyMs) && recentPlaybackOutputLatencyMs > 0
            ? recentPlaybackOutputLatencyMs
            : 0;
    }

    public bool UsesPlaybackLeakageStartGuard => _captureStartContext != SpeechCaptureStartContext.CleanListening;

    public double RecentPlaybackSpeechStartGuardMs
    {
        get
        {
            if (_captureStartContext != SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle
                && _captureStartContext != SpeechCaptureStartContext.AssistantPlaybackRecentlySettledContinuation)
            {
                return 0;
            }

            var baseGuardMs = Math.Max(0, _options.RecentPlaybackSpeechStartGuard.TotalMilliseconds);

            // Die Ausgangspuffer können nach dem Wiedergabeende noch hörbare Sprache enthalten.
            // Dafür die Schutzzeit bei Bedarf auf Ausgangslatenz plus Zeitreserve verlängern.
            // Das Maximum erhält die konfigurierte Mindestschutzzeit auch bei kleiner Latenz.
            var latencyCoveredGuardMs = _recentPlaybackOutputLatencyMs > 0
                ? _recentPlaybackOutputLatencyMs + Math.Max(0, _options.OutputLatencyGuardMargin.TotalMilliseconds)
                : 0;

            return Math.Max(baseGuardMs, latencyCoveredGuardMs);
        }
    }

    public bool UsesAdaptiveSpeechFloor()
    {
        return UsesActivePlaybackCalibration() || UsesIdleRecentPlaybackCalibration();
    }

    public bool AcceptsStartupCalibrationSample(int averageAmplitude)
    {
        if (UsesActivePlaybackCalibration())
        {
            return true;
        }

        if (UsesIdleRecentPlaybackCalibration())
        {
            return averageAmplitude < _options.PlaybackLeakageBypassAmplitudeThreshold;
        }

        return false;
    }

    public bool IsBelowPlaybackLeakageBypassThreshold(int averageAmplitude)
    {
        return averageAmplitude < _options.PlaybackLeakageBypassAmplitudeThreshold;
    }

    public bool ShouldDropBufferedPreRollOnSpeechStart()
    {
        return _captureStartContext == SpeechCaptureStartContext.AssistantPlaybackActive;
    }

    public int GetCalibrationChunkCount()
    {
        if (UsesActivePlaybackCalibration())
        {
            return Math.Max(0, _options.ActivePlaybackCalibrationChunkCount);
        }

        if (UsesIdleRecentPlaybackCalibration())
        {
            return Math.Max(0, _options.RecentPlaybackCalibrationChunkCount);
        }

        return 0;
    }

    public int ResolveSpeechStartAdaptiveMargin()
    {
        if (UsesActivePlaybackCalibration())
        {
            return Math.Max(
                _options.SpeechStartAdaptiveMargin,
                _options.ActivePlaybackSpeechStartAdaptiveMargin);
        }

        return _options.SpeechStartAdaptiveMargin;
    }

    /// <summary>
    /// Bestimmt aus dem geschätzten Hintergrundpegel <paramref name="estimatedFloor"/>
    /// und dem kontextabhängigen Abstand eine Sprachbeginnschwelle. Sie liegt mindestens
    /// bei <c>SpeechAmplitudeThreshold</c>. Nur im Leerlauf nach Wiedergabe begrenzt
    /// <see cref="Math.Min"/> sie zusätzlich auf die dafür vorgesehene Obergrenze.
    /// </summary>
    public int ResolveAdaptiveOnsetThreshold(int estimatedFloor)
    {
        var threshold = Math.Max(
            _options.SpeechAmplitudeThreshold,
            estimatedFloor + ResolveSpeechStartAdaptiveMargin());

        return ResolveIdleAdaptiveFloorCeiling() is int ceiling
            ? Math.Min(threshold, ceiling)
            : threshold;
    }

    /// <summary>
    /// Liefert die Obergrenze der adaptiven Sprachbeginnschwelle im Leerlauf nach Wiedergabe.
    /// Sie beträgt <c>SpeechAmplitudeThreshold + IdleAdaptiveFloorMaxMargin</c>, wobei
    /// negative Abstände als 0 gelten. Die Grenze liegt damit nicht unter
    /// <c>SpeechAmplitudeThreshold</c>. Für andere Startkontexte wird <c>null</c> geliefert.
    /// </summary>
    public int? ResolveIdleAdaptiveFloorCeiling()
    {
        if (!UsesIdleRecentPlaybackCalibration())
        {
            return null;
        }

        return _options.SpeechAmplitudeThreshold + Math.Max(0, _options.IdleAdaptiveFloorMaxMargin);
    }

    private bool UsesActivePlaybackCalibration()
    {
        return _captureStartContext == SpeechCaptureStartContext.AssistantPlaybackActive;
    }

    private bool UsesIdleRecentPlaybackCalibration()
    {
        return _captureStartContext == SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle;
    }
}
