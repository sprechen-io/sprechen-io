using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Koordiniert die fortlaufende Segmenterfassung mit gemeinsamem Lesecursor
/// und überwacht den Eingang neuer Audioblöcke, damit Segmente am bisherigen Pufferstand anschließen.
/// </summary>
internal sealed class ActiveSegmentCoordinator : ISpeechCaptureChunkConsumer, IDisposable
{
    // Ohne übergebene Optionen gilt dieselbe Standardfrist für eingehende Audioblöcke
    // wie bei der Initialisierung mit IOptions.
    private static readonly TimeSpan DefaultChunkLivenessTimeout = new SpeechCaptureClientOptions().ChunkArrivalLivenessTimeout;

    private readonly SpeechCaptureSegmenterFactory _segmenterFactory;
    private readonly SpeechSampleRing _sampleRing;
    private readonly ActiveCaptureTrace _trace;
    private readonly ActiveSegmentSlotStore _slotStore = new();
    private readonly ActiveSegmentTerminalSignal _terminalSignal;
    private readonly ContinuousSegmentCursorBacklogDrainer _cursorBacklogDrainer;
    private readonly ActiveSegmentChunkFlow _chunkFlow;
    private readonly CaptureChunkLivenessGuard _chunkLiveness;
    private readonly CaptureTerminalFlow _terminalFlow;

    public ActiveSegmentCoordinator(
        SpeechCaptureSegmenterFactory segmenterFactory,
        SpeechSampleRing sampleRing,
        IBrowserDebugConsole browserDebugConsole)
        : this(
            segmenterFactory,
            sampleRing,
            browserDebugConsole,
            DefaultChunkLivenessTimeout)
    {
    }

    public ActiveSegmentCoordinator(
        SpeechCaptureSegmenterFactory segmenterFactory,
        SpeechSampleRing sampleRing,
        IBrowserDebugConsole browserDebugConsole,
        IOptions<SpeechCaptureClientOptions> captureOptions)
        : this(
            segmenterFactory,
            sampleRing,
            browserDebugConsole,
            ResolveLivenessTimeout(captureOptions))
    {
    }

    private ActiveSegmentCoordinator(
        SpeechCaptureSegmenterFactory segmenterFactory,
        SpeechSampleRing sampleRing,
        IBrowserDebugConsole browserDebugConsole,
        TimeSpan chunkLivenessTimeout)
    {
        ArgumentNullException.ThrowIfNull(browserDebugConsole);

        _segmenterFactory = segmenterFactory ?? throw new ArgumentNullException(nameof(segmenterFactory));
        _sampleRing = sampleRing ?? throw new ArgumentNullException(nameof(sampleRing));
        _trace = new ActiveCaptureTrace(browserDebugConsole, NullLogger<ActiveSegmentCoordinator>.Instance);
        _chunkLiveness = new CaptureChunkLivenessGuard(
            chunkLivenessTimeout > TimeSpan.Zero ? chunkLivenessTimeout : DefaultChunkLivenessTimeout,
            browserDebugConsole,
            "capture segment stalled; no chunk within liveness window");
        _terminalSignal = new ActiveSegmentTerminalSignal(_slotStore);
        _cursorBacklogDrainer = new ContinuousSegmentCursorBacklogDrainer(_slotStore, _sampleRing, browserDebugConsole);
        _chunkFlow = new ActiveSegmentChunkFlow(_slotStore, browserDebugConsole);
        _terminalFlow = new CaptureTerminalFlow(_trace);
    }

    private static TimeSpan ResolveLivenessTimeout(IOptions<SpeechCaptureClientOptions> captureOptions)
    {
        ArgumentNullException.ThrowIfNull(captureOptions);
        return captureOptions.Value.ChunkArrivalLivenessTimeout;
    }

    public void ResetToLiveEdge()
    {
        _slotStore.ResetToLiveEdge(_sampleRing);
    }

    internal long BeginCaptureAttempt() => _slotStore.BeginAttempt();

    internal void EndCaptureAttempt(long attemptId) => _slotStore.EndAttempt(attemptId);

    public Task<CapturedSpeechAudio> CaptureNextSegmentAsync(
        SpeechAudioMimeType mimeType,
        int silenceDebounceMs,
        SpeechCaptureStartContext captureStartContext,
        SpeechCaptureModeBehavior modeBehavior,
        CancellationToken cancellationToken,
        long attemptId = 0)
        => _terminalFlow.RunAsync(
            () => CaptureArmedSegmentAsync(
                mimeType,
                silenceDebounceMs,
                captureStartContext,
                modeBehavior,
                attemptId,
                cancellationToken),
            cancellationToken);

    private async Task<CapturedSpeechAudio> CaptureArmedSegmentAsync(
        SpeechAudioMimeType mimeType,
        int silenceDebounceMs,
        SpeechCaptureStartContext captureStartContext,
        SpeechCaptureModeBehavior modeBehavior,
        long attemptId,
        CancellationToken cancellationToken)
    {
        await _trace
            .RecordStartAsync(silenceDebounceMs, captureStartContext, modeBehavior)
            .ConfigureAwait(false);

        ActiveSegmentSlot? armedSlot = null;
        try
        {
            var armedSegmenter = _segmenterFactory.CreateSegmenter(
                mimeType,
                silenceDebounceMs,
                captureStartContext,
                modeBehavior,
                initialSilenceTimeout: null);
            armedSlot = _slotStore.Arm(armedSegmenter, attemptId);
            var activeSlot = armedSlot;
            activeSlot.RegisterCancellation(cancellationToken, _terminalSignal.CancelSlot);

            await _cursorBacklogDrainer.DrainAsync(activeSlot, cancellationToken).ConfigureAwait(false);
            await _trace.RecordArmedAsync().ConfigureAwait(false);

            var captured = await _chunkLiveness
                .AwaitAsync(activeSlot.CaptureTask, exception => activeSlot.TryFail(exception), cancellationToken)
                .ConfigureAwait(false);
            return await _terminalFlow.CompleteAsync(activeSlot, captured).ConfigureAwait(false);
        }
        finally
        {
            _terminalFlow.Release(armedSlot, slot => _slotStore.Clear(slot));
        }
    }

    public async Task ProcessChunkAsync(byte[] chunk, CancellationToken cancellationToken)
    {
        _chunkLiveness.ObserveChunk();
        await _chunkFlow.ProcessChunkAsync(chunk, cancellationToken).ConfigureAwait(false);
    }

    public void FailActiveCapture(Exception ex)
    {
        _terminalSignal.FailActiveCapture(ex);
    }

    public void CancelActiveCapture(CaptureCancellationKind kind, CancellationToken cancellationToken)
    {
        _terminalSignal.CancelActiveCapture(kind, cancellationToken);
    }

    public void Dispose()
    {
        _terminalSignal.DisposeActiveSegment();
        _terminalFlow.Dispose();
    }
}
