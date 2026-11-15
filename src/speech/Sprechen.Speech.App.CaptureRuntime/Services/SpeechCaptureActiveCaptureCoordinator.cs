using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Verbindet Aufnahmeaufrufe, Pegelauflösung und eintreffende Blöcke mit einem Laufzeitzustand,
/// damit alle Zugänge denselben aktiven Versuch und dessen Abschluss verwenden.
/// </summary>
internal sealed class SpeechCaptureActiveCaptureCoordinator : ISpeechCaptureChunkConsumer, IDisposable
{
    private static readonly TimeSpan DefaultChunkLivenessTimeout =
        new SpeechCaptureClientOptions().ChunkArrivalLivenessTimeout;
    private readonly ActiveCaptureRuntimeGraph _runtimeGraph;
    private readonly SpeechSampleRing _sampleRing;
    private readonly SpeechCaptureSpeechAmplitudeThresholdResolver _speechAmplitudeThresholdResolver;

    public SpeechCaptureActiveCaptureCoordinator(
        IUserSpeechBargeInHandler bargeInHandler,
        ILogger<SpeechCaptureActiveCaptureCoordinator> logger,
        IBrowserDebugConsole browserDebugConsole,
        SpeechCaptureSegmenterFactory segmenterFactory,
        SpeechSampleRing sampleRing,
        IMicGateState micGateState,
        ICaptureDiagnosticsReporter? captureDiagnostics = null,
        SpeechCaptureSpeechAmplitudeThresholdResolver? speechAmplitudeThresholdResolver = null,
        IOptions<SpeechCaptureClientOptions>? captureOptions = null)
    {
        _sampleRing = sampleRing ?? throw new ArgumentNullException(nameof(sampleRing));
        _speechAmplitudeThresholdResolver = speechAmplitudeThresholdResolver
            ?? new SpeechCaptureSpeechAmplitudeThresholdResolver();
        _runtimeGraph = ActiveCaptureRuntimeGraph.Create(
            bargeInHandler,
            logger,
            browserDebugConsole,
            segmenterFactory,
            sampleRing,
            micGateState,
            ResolveLivenessTimeout(captureOptions),
            new ActiveCaptureTrace(browserDebugConsole, logger, captureDiagnostics));
    }

    private static TimeSpan ResolveLivenessTimeout(IOptions<SpeechCaptureClientOptions>? captureOptions)
    {
        var configured = captureOptions?.Value.ChunkArrivalLivenessTimeout ?? DefaultChunkLivenessTimeout;
        return configured > TimeSpan.Zero ? configured : DefaultChunkLivenessTimeout;
    }

    public SampleCursor OpenLiveCaptureCursor() => _sampleRing.OpenCursor(SampleCursorOrigin.Latest);

    public Task<CapturedSpeechAudio> CaptureAsync(
        SpeechAudioMimeType mimeType,
        int silenceDebounceMs,
        SpeechCaptureStartContext captureStartContext,
        SpeechCaptureModeBehavior behavior,
        TimeSpan? initialSilenceTimeout,
        CancellationToken cancellationToken)
        => CaptureAsync(
            mimeType,
            silenceDebounceMs,
            captureStartContext,
            behavior,
            initialSilenceTimeout,
            startCursor: null,
            cancellationToken);

    public Task<CapturedSpeechAudio> CaptureAsync(
        SpeechAudioMimeType mimeType,
        int silenceDebounceMs,
        SpeechCaptureStartContext captureStartContext,
        SpeechCaptureModeBehavior behavior,
        TimeSpan? initialSilenceTimeout,
        SampleCursor? startCursor,
        CancellationToken cancellationToken)
    {
        var admission = AdmitCaptureAttempt();
        return CaptureWithResolvedThresholdAsync(
            admission,
            mimeType,
            silenceDebounceMs,
            captureStartContext,
            behavior,
            initialSilenceTimeout,
            startCursor,
            cancellationToken);
    }

    internal CaptureAttemptAdmission AdmitCaptureAttempt() => _runtimeGraph.AttemptFlow.AdmitAttempt();

    internal Task<CapturedSpeechAudio> CaptureAdmittedAsync(
        CaptureAttemptAdmission admission,
        SpeechAudioMimeType mimeType,
        int silenceDebounceMs,
        SpeechCaptureStartContext captureStartContext,
        SpeechCaptureModeBehavior behavior,
        TimeSpan? initialSilenceTimeout,
        SampleCursor? startCursor,
        CancellationToken cancellationToken)
        => CaptureWithResolvedThresholdAsync(
            admission,
            mimeType,
            silenceDebounceMs,
            captureStartContext,
            behavior,
            initialSilenceTimeout,
            startCursor,
            cancellationToken);

    internal void ThrowIfCaptureAdmissionInvalid(CaptureAttemptAdmission admission) =>
        _runtimeGraph.AttemptFlow.ThrowIfAdmissionInvalid(admission);

    private async Task<CapturedSpeechAudio> CaptureWithResolvedThresholdAsync(
        CaptureAttemptAdmission admission,
        SpeechAudioMimeType mimeType,
        int silenceDebounceMs,
        SpeechCaptureStartContext captureStartContext,
        SpeechCaptureModeBehavior behavior,
        TimeSpan? initialSilenceTimeout,
        SampleCursor? startCursor,
        CancellationToken cancellationToken)
    {
        var speechAmplitudeThreshold = await _speechAmplitudeThresholdResolver
            .GetSpeechAmplitudeThresholdAsync(cancellationToken)
            .ConfigureAwait(false);

        return await _runtimeGraph.AttemptFlow.CaptureAsync(
            new AdmittedCaptureAttempt(admission, speechAmplitudeThreshold),
            mimeType,
            silenceDebounceMs,
            captureStartContext,
            behavior,
            initialSilenceTimeout,
            startCursor,
            cancellationToken)
            .ConfigureAwait(false);
    }

    public Task ProcessChunkAsync(byte[] chunk, CancellationToken cancellationToken)
        => _runtimeGraph.ChunkFlow.ProcessChunkAsync(chunk, cancellationToken);

    public void FailActiveCapture(Exception ex)
    {
        _runtimeGraph.TerminalSignal.FailActiveCapture(ex);
    }

    public void CancelActiveCapture(CaptureCancellationKind kind, CancellationToken cancellationToken)
    {
        _runtimeGraph.TerminalSignal.CancelActiveCapture(kind, cancellationToken);
    }

    public void Dispose()
    {
        _runtimeGraph.Dispose();
    }
}
