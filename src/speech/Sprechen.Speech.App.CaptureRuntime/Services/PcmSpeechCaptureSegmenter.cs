namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Segmentiert PCM-Audioblöcke in Äußerungen, damit Sprachbeginn, Vorlauf
/// und Abschluss nach einer Stillepause einheitlich bestimmt werden.
/// </summary>
internal sealed class PcmSpeechCaptureSegmenter
{
    private readonly PcmSpeechCaptureSegmenterOptions _options;
    private readonly SpeechAudioMimeType _mimeType;
    private readonly int _silenceDebounceMs;
    private readonly PcmAudioChunkAnalyzer _chunkAnalyzer;
    private readonly PcmSpeechCapturePeakLevel _peakLevel = new();
    private readonly PcmSpeechCaptureStartContextPolicy _startContextPolicy;
    private readonly PcmSpeechCaptureBufferedAudioState _bufferedAudioState;
    private readonly PcmSpeechCaptureProgressState _progressState;
    private readonly PcmSpeechCaptureCandidateState _candidateState;
    private readonly PcmSpeechCaptureConfirmedSpeechFlow _confirmedSpeechFlow;
    private readonly PcmSpeechCaptureInitialSilenceTimeoutRule _initialSilenceTimeoutRule;

    public PcmSpeechCaptureSegmenter(
        SpeechAudioMimeType mimeType,
        int silenceDebounceMs,
        PcmSpeechCaptureSegmenterOptions options)
        : this(mimeType, silenceDebounceMs, SpeechCaptureStartContext.CleanListening, options)
    {
    }

    public PcmSpeechCaptureSegmenter(
        SpeechAudioMimeType mimeType,
        int silenceDebounceMs,
        SpeechCaptureStartContext captureStartContext,
        PcmSpeechCaptureSegmenterOptions options,
        double recentPlaybackOutputLatencyMs = 0)
    {
        if (silenceDebounceMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(silenceDebounceMs), silenceDebounceMs, "Silence debounce must be positive.");
        }

        _mimeType = mimeType;
        _silenceDebounceMs = silenceDebounceMs;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _chunkAnalyzer = new PcmAudioChunkAnalyzer(_options);
        _startContextPolicy = new PcmSpeechCaptureStartContextPolicy(
            captureStartContext,
            _options,
            recentPlaybackOutputLatencyMs);
        _bufferedAudioState = new PcmSpeechCaptureBufferedAudioState(_options.MaxPreRollChunks, _options.InitialChunkCapacity);
        _progressState = new PcmSpeechCaptureProgressState(_startContextPolicy.RecentPlaybackSpeechStartGuardMs);
        _candidateState = new PcmSpeechCaptureCandidateState(_startContextPolicy.UsesPlaybackLeakageStartGuard);
        _confirmedSpeechFlow = new PcmSpeechCaptureConfirmedSpeechFlow(
            _options,
            _bufferedAudioState,
            _progressState,
            _mimeType,
            _silenceDebounceMs);
        _initialSilenceTimeoutRule = new PcmSpeechCaptureInitialSilenceTimeoutRule(_options, _progressState);
    }
    internal PcmSpeechCapturePeakLevel PeakLevel => _peakLevel;

    public PcmSpeechCaptureSegmenterUpdate ProcessChunk(byte[] chunk)
    {
        if (chunk is null || chunk.Length == 0)
        {
            return default;
        }

        var analysis = _chunkAnalyzer.Analyze(chunk);
        _peakLevel.Observe(analysis.AverageAmplitude);

        if (!_candidateState.SpeechConfirmed)
        {
            _progressState.AdvanceWaitForSpeech(analysis.DurationMs);
        }

        if (!_candidateState.SpeechStarted)
        {
            return ProcessWaitingForSpeech(chunk, analysis);
        }

        if (!_candidateState.SpeechConfirmed)
        {
            return ProcessUnconfirmedSpeech(chunk, analysis);
        }

        return _confirmedSpeechFlow.Process(chunk, analysis);
    }

    private PcmSpeechCaptureSegmenterUpdate ProcessWaitingForSpeech(byte[] chunk, PcmAudioChunkAnalysis analysis)
    {
        if (ShouldDelaySpeechStartForRecentPlayback(analysis.DurationMs))
        {
            return EnqueuePreRollOrNoSpeech(chunk, analysis.AverageAmplitude, trackCalibration: false);
        }

        if (ShouldCollectStartupCalibrationSample(analysis.AverageAmplitude))
        {
            return EnqueuePreRollOrNoSpeech(chunk, analysis.AverageAmplitude);
        }

        if (!TryResolveSpeechStartThreshold(analysis.AverageAmplitude, out var speechStartThreshold))
        {
            return EnqueuePreRollOrNoSpeech(chunk, analysis.AverageAmplitude);
        }

        return BeginSpeechCandidate(chunk, analysis.DurationMs, speechStartThreshold);
    }

    private PcmSpeechCaptureSegmenterUpdate ProcessUnconfirmedSpeech(byte[] chunk, PcmAudioChunkAnalysis analysis)
    {
        if (!MeetsSpeechThreshold(analysis.AverageAmplitude, _candidateState.CandidateSpeechThreshold))
        {
            ResetUnconfirmedSpeechState();
            return EnqueuePreRollOrNoSpeech(chunk, analysis.AverageAmplitude);
        }

        _bufferedAudioState.AddChunk(chunk);
        _progressState.ContinueUnconfirmedSpeech(analysis.DurationMs);
        _bufferedAudioState.MarkTailEndAtCurrentChunk();

        return TryConfirmSpeechStart();
    }

    /// <summary>
    /// Liest ab <paramref name="cursor"/> aus dem Ring und verarbeitet die Bytes mit
    /// <see cref="ProcessChunk(byte[])"/>. Der Cursor rückt um die gelesene Bytezahl vor,
    /// damit der nächste Aufruf unmittelbar anschließen kann.
    /// </summary>
    public PcmSpeechCaptureSegmenterUpdate ReadFrom(SpeechSampleRing ring, ref SampleCursor cursor)
    {
        // PcmRingChunkReader gibt eine eigene Kopie zurück, damit die Aufnahme die Daten behalten kann.
        var chunk = PcmRingChunkReader.ReadRightSizedChunk(ring, ref cursor, ReadFromMaxBytesPerCall);
        return chunk is null ? default : ProcessChunk(chunk);
    }

    /// <summary>
    /// Liefert die bisher erfassten Audioblöcke als <see cref="CapturedSpeechAudio"/>,
    /// auch wenn die abschließende Stillepause noch nicht erreicht ist. Ohne laufende
    /// Audioblöcke wird <c>null</c> zurückgegeben. Dies ermöglicht den Abschluss
    /// mit <c>CaptureCancellationKind.Drain</c>.
    /// </summary>
    public CapturedSpeechAudio? FlushPartial()
    {
        if (!_bufferedAudioState.HasInFlightChunks)
        {
            return null;
        }

        return _bufferedAudioState.BuildPartialCapturedUtterance(_mimeType);
    }

    private const int NominalReadFrameMs = 20;

    private int ReadFromMaxBytesPerCall
    {
        get
        {
            var bytesPerFrame = _options.PcmSampleRateHz * _options.Pcm16BytesPerSample * NominalReadFrameMs / 1000;
            return Math.Max(_options.Pcm16BytesPerSample, bytesPerFrame);
        }
    }

    private void EnqueuePreRoll(byte[] chunk, int averageAmplitude, bool trackCalibration = true)
    {
        if (trackCalibration)
        {
            var calibrationChunkCount = _startContextPolicy.GetCalibrationChunkCount();
            _candidateState.RecordStartupCalibrationSample(calibrationChunkCount);
        }

        _bufferedAudioState.EnqueuePreRoll(chunk, averageAmplitude, trackCalibration, _options.MaxPreRollChunks);
    }

    private PcmSpeechCaptureSegmenterUpdate EnqueuePreRollOrNoSpeech(
        byte[] chunk,
        int averageAmplitude,
        bool trackCalibration = true)
    {
        EnqueuePreRoll(chunk, averageAmplitude, trackCalibration);
        return _initialSilenceTimeoutRule.BuildUpdate();
    }

    private PcmSpeechCaptureSegmenterUpdate BeginSpeechCandidate(
        byte[] chunk,
        double durationMs,
        int speechStartThreshold)
    {
        _candidateState.BeginSpeechCandidate(speechStartThreshold);
        _progressState.BeginSpeechCandidate(durationMs);

        if (ShouldDropBufferedPreRollOnSpeechStart())
        {
            _bufferedAudioState.ClearPreRollAndCalibration();
        }

        _bufferedAudioState.DrainPreRollIntoChunks();
        _bufferedAudioState.AddChunk(chunk);
        _bufferedAudioState.MarkTailEndAtCurrentChunk();

        return TryConfirmSpeechStart();
    }

    private PcmSpeechCaptureSegmenterUpdate TryConfirmSpeechStart()
    {
        if (!_candidateState.TryConfirmSpeechStart(_progressState.ConfirmedSpeechMs, _options.SpeechStartConfirmation.TotalMilliseconds))
        {
            return default;
        }

        return new PcmSpeechCaptureSegmenterUpdate(true, null, null);
    }

    private void ResetUnconfirmedSpeechState()
    {
        _candidateState.ResetUnconfirmedSpeechState();
        _progressState.ResetUnconfirmedSpeechState();
        _bufferedAudioState.ReturnChunksToPreRoll(_options.MaxPreRollChunks);
    }

    private bool TryResolveSpeechStartThreshold(int averageAmplitude, out int speechStartThreshold)
    {
        if (_candidateState.TryConsumePlaybackLeakageGuard())
        {
            if (_startContextPolicy.IsBelowPlaybackLeakageBypassThreshold(averageAmplitude))
            {
                speechStartThreshold = 0;
                return false;
            }
        }

        speechStartThreshold = ResolveSpeechStartThreshold();
        return MeetsSpeechThreshold(averageAmplitude, speechStartThreshold);
    }

    private bool ShouldDelaySpeechStartForRecentPlayback(double durationMs)
    {
        return _progressState.TryDelaySpeechStartForRecentPlayback(durationMs);
    }

    private bool ShouldCollectStartupCalibrationSample(int averageAmplitude)
    {
        var calibrationChunkCount = _startContextPolicy.GetCalibrationChunkCount();
        if (calibrationChunkCount == 0)
        {
            return false;
        }

        return _candidateState.ShouldCollectStartupCalibrationSample(calibrationChunkCount)
               && _startContextPolicy.AcceptsStartupCalibrationSample(averageAmplitude);
    }

    private bool ShouldDropBufferedPreRollOnSpeechStart()
    {
        return _startContextPolicy.ShouldDropBufferedPreRollOnSpeechStart();
    }

    private int ResolveSpeechStartThreshold()
        => _peakLevel.ResolveOnsetThreshold(
            _startContextPolicy,
            _bufferedAudioState,
            _options.SpeechAmplitudeThreshold);

    private static bool MeetsSpeechThreshold(int averageAmplitude, int threshold)
    {
        return averageAmplitude >= threshold;
    }
}
