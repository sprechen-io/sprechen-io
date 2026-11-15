namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verfolgt Warte-, Sprach- und Stillezeiten einer Segmentierung,
/// damit Zeitgrenzen und Schutzfristen denselben Verarbeitungsfortschritt verwenden.
/// </summary>
internal sealed class PcmSpeechCaptureProgressState
{
    private double _waitForSpeechMs;
    private double _confirmedSpeechMs;
    private double _speechMs;
    private double _utteranceMs;
    private double _silenceAfterSpeechMs;
    private double _tailSilenceMs;
    private double _recentPlaybackSpeechStartGuardMs;

    public PcmSpeechCaptureProgressState(double recentPlaybackSpeechStartGuardMs)
    {
        _recentPlaybackSpeechStartGuardMs = recentPlaybackSpeechStartGuardMs;
    }

    public double ConfirmedSpeechMs => _confirmedSpeechMs;
    public double SpeechMs => _speechMs;
    public double UtteranceMs => _utteranceMs;
    public double SilenceAfterSpeechMs => _silenceAfterSpeechMs;
    public double TailSilenceMs => _tailSilenceMs;

    public void AdvanceWaitForSpeech(double durationMs)
    {
        _waitForSpeechMs += durationMs;
    }

    public bool HasReachedInitialSilenceTimeout(double maxInitialSilenceMs)
    {
        return _waitForSpeechMs >= maxInitialSilenceMs;
    }

    public bool TryDelaySpeechStartForRecentPlayback(double durationMs)
    {
        if (_recentPlaybackSpeechStartGuardMs <= 0)
        {
            return false;
        }

        _recentPlaybackSpeechStartGuardMs = Math.Max(0, _recentPlaybackSpeechStartGuardMs - durationMs);
        return true;
    }

    public void BeginSpeechCandidate(double durationMs)
    {
        // Die Wartezeit bei einem neuen Sprachkandidaten zurücksetzen, damit vorherige Stille
        // dessen Zeitlimit nicht verbraucht. Nach einem Fehlstart wird sie erneut fortgeschrieben.
        _waitForSpeechMs = 0;
        _confirmedSpeechMs = durationMs;
        _speechMs = durationMs;
        _utteranceMs = durationMs;
        _silenceAfterSpeechMs = 0;
        _tailSilenceMs = 0;
    }

    public void ContinueUnconfirmedSpeech(double durationMs)
    {
        _confirmedSpeechMs += durationMs;
        _speechMs += durationMs;
        _utteranceMs += durationMs;
        _silenceAfterSpeechMs = 0;
        _tailSilenceMs = 0;
    }

    public void ContinueConfirmedSpeech(double durationMs)
    {
        _speechMs += durationMs;
        _utteranceMs += durationMs;
        _silenceAfterSpeechMs = 0;
        _tailSilenceMs = 0;
    }

    public void ContinueConfirmedSilence(double durationMs)
    {
        _utteranceMs += durationMs;
        _silenceAfterSpeechMs += durationMs;
    }

    public bool TryExtendTailSilence(double durationMs, double maxTailSilenceMs)
    {
        if (_tailSilenceMs >= maxTailSilenceMs)
        {
            return false;
        }

        _tailSilenceMs += durationMs;
        return _tailSilenceMs <= maxTailSilenceMs;
    }

    public void ResetUnconfirmedSpeechState()
    {
        _confirmedSpeechMs = 0;
        _speechMs = 0;
        _utteranceMs = 0;
        _silenceAfterSpeechMs = 0;
        _tailSilenceMs = 0;
    }
}
