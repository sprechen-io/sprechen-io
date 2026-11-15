namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verfolgt vermuteten und bestätigten Sprachbeginn samt Kalibrierung und Ausgabeschutz,
/// damit kurze Fehlansätze nicht unmittelbar als bestätigte Sprache gelten.
/// </summary>
internal sealed class PcmSpeechCaptureCandidateState
{
    private int _candidateSpeechThreshold;
    private int _startupCalibrationChunkCount;
    private bool _speechStarted;
    private bool _speechConfirmed;
    private bool _playbackLeakageGuardPending;

    public PcmSpeechCaptureCandidateState(bool playbackLeakageGuardPending)
    {
        _playbackLeakageGuardPending = playbackLeakageGuardPending;
    }

    public int CandidateSpeechThreshold => _candidateSpeechThreshold;
    public bool SpeechStarted => _speechStarted;
    public bool SpeechConfirmed => _speechConfirmed;

    public bool TryConsumePlaybackLeakageGuard()
    {
        if (!_playbackLeakageGuardPending)
        {
            return false;
        }

        _playbackLeakageGuardPending = false;
        return true;
    }

    public bool ShouldCollectStartupCalibrationSample(int calibrationChunkCount)
    {
        return calibrationChunkCount > 0 && _startupCalibrationChunkCount < calibrationChunkCount;
    }

    public void RecordStartupCalibrationSample(int calibrationChunkCount)
    {
        if (calibrationChunkCount > 0 && _startupCalibrationChunkCount < calibrationChunkCount)
        {
            _startupCalibrationChunkCount++;
        }
    }

    public void BeginSpeechCandidate(int speechStartThreshold)
    {
        _speechStarted = true;
        _speechConfirmed = false;
        _candidateSpeechThreshold = speechStartThreshold;
    }

    public bool TryConfirmSpeechStart(double confirmedSpeechMs, double requiredConfirmationMs)
    {
        if (_speechConfirmed || confirmedSpeechMs < requiredConfirmationMs)
        {
            return false;
        }

        _speechConfirmed = true;
        return true;
    }

    public void ResetUnconfirmedSpeechState()
    {
        _speechStarted = false;
        _speechConfirmed = false;
        _candidateSpeechThreshold = 0;
    }
}
