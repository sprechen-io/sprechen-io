namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Führt bestätigte Sprache bis zur Stilleschwelle oder Längengrenze fort,
/// damit Stille den vorgesehenen Nachlauf begrenzt und lange Äußerungen trotzdem abgeschlossen werden.
/// </summary>
internal sealed class PcmSpeechCaptureConfirmedSpeechFlow
{
    private readonly PcmSpeechCaptureSegmenterOptions _options;
    private readonly PcmSpeechCaptureBufferedAudioState _bufferedAudioState;
    private readonly PcmSpeechCaptureProgressState _progressState;
    private readonly SpeechAudioMimeType _mimeType;
    private readonly int _silenceDebounceMs;

    internal PcmSpeechCaptureConfirmedSpeechFlow(
        PcmSpeechCaptureSegmenterOptions options,
        PcmSpeechCaptureBufferedAudioState bufferedAudioState,
        PcmSpeechCaptureProgressState progressState,
        SpeechAudioMimeType mimeType,
        int silenceDebounceMs)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _bufferedAudioState = bufferedAudioState ?? throw new ArgumentNullException(nameof(bufferedAudioState));
        _progressState = progressState ?? throw new ArgumentNullException(nameof(progressState));
        _mimeType = mimeType;
        _silenceDebounceMs = silenceDebounceMs;
    }

    internal PcmSpeechCaptureSegmenterUpdate Process(byte[] chunk, PcmAudioChunkAnalysis analysis)
    {
        _bufferedAudioState.AddChunk(chunk);

        var chunkIndex = _bufferedAudioState.ChunkCount - 1;
        var completedUtterance = ResolveSilenceCompletion(chunkIndex, analysis);

        if (completedUtterance is null && HasReachedSegmentLimit())
        {
            completedUtterance = BuildCapturedUtterance();
        }

        return completedUtterance is null
            ? default
            : new PcmSpeechCaptureSegmenterUpdate(false, completedUtterance, null);
    }

    private CapturedSpeechAudio? ResolveSilenceCompletion(int chunkIndex, PcmAudioChunkAnalysis analysis)
    {
        if (IsSpeechAmplitude(analysis.AverageAmplitude))
        {
            _progressState.ContinueConfirmedSpeech(analysis.DurationMs);
            _bufferedAudioState.MarkTailEndAt(chunkIndex);
            return null;
        }

        _progressState.ContinueConfirmedSilence(analysis.DurationMs);

        if (_progressState.TryExtendTailSilence(analysis.DurationMs, _options.TailSilence.TotalMilliseconds))
        {
            _bufferedAudioState.MarkTailEndAt(chunkIndex);
        }

        return _progressState.SilenceAfterSpeechMs >= _silenceDebounceMs
            ? BuildCapturedUtterance()
            : null;
    }

    private bool HasReachedSegmentLimit()
    {
        return _progressState.SpeechMs >= _options.MaxUtteranceDuration.TotalMilliseconds
            || _progressState.UtteranceMs >= _options.MaxTotalSegmentDuration.TotalMilliseconds;
    }

    private CapturedSpeechAudio BuildCapturedUtterance()
    {
        return _bufferedAudioState.BuildCapturedUtterance(_mimeType);
    }

    private bool IsSpeechAmplitude(int averageAmplitude)
    {
        return averageAmplitude >= _options.SpeechAmplitudeThreshold;
    }
}
