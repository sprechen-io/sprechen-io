using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Baut Versuchsdurchführung, Blockverarbeitung und Abschlusssignale mit gemeinsamem Zustand auf,
/// damit die beteiligten Aufnahmewege denselben aktiven Versuch verwalten. Die fertige Spur wird
/// übergeben, damit die Komposition die Diagnosezugänge nicht selbst kennen muss.
/// </summary>
internal sealed class ActiveCaptureRuntimeGraph : IDisposable
{
    private ActiveCaptureRuntimeGraph(
        ActiveCaptureAttemptFlow attemptFlow,
        ActiveCaptureChunkFlow chunkFlow,
        ActiveCaptureTerminalSignal terminalSignal)
    {
        AttemptFlow = attemptFlow ?? throw new ArgumentNullException(nameof(attemptFlow));
        ChunkFlow = chunkFlow ?? throw new ArgumentNullException(nameof(chunkFlow));
        TerminalSignal = terminalSignal ?? throw new ArgumentNullException(nameof(terminalSignal));
    }

    internal ActiveCaptureAttemptFlow AttemptFlow { get; }

    internal ActiveCaptureChunkFlow ChunkFlow { get; }

    internal ActiveCaptureTerminalSignal TerminalSignal { get; }

    internal static ActiveCaptureRuntimeGraph Create(
        IUserSpeechBargeInHandler bargeInHandler,
        ILogger logger,
        IBrowserDebugConsole browserDebugConsole,
        SpeechCaptureSegmenterFactory segmenterFactory,
        SpeechSampleRing sampleRing,
        IMicGateState micGateState,
        TimeSpan chunkLivenessTimeout,
        ActiveCaptureTrace trace)
    {
        ArgumentNullException.ThrowIfNull(bargeInHandler);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(browserDebugConsole);
        ArgumentNullException.ThrowIfNull(segmenterFactory);
        ArgumentNullException.ThrowIfNull(sampleRing);
        ArgumentNullException.ThrowIfNull(micGateState);
        ArgumentNullException.ThrowIfNull(trace);

        var slotStore = new ActiveCaptureSlotStore(sampleRing);
        var terminalSignal = new ActiveCaptureTerminalSignal(slotStore);
        var chunkLiveness = new CaptureChunkLivenessGuard(
            chunkLivenessTimeout,
            browserDebugConsole,
            "capture stalled; no chunk within liveness window");
        var cursorBacklogDrainer = new ActiveCaptureCursorBacklogDrainer(slotStore, browserDebugConsole);
        var chunkFlow = new ActiveCaptureChunkFlow(
            slotStore,
            bargeInHandler,
            logger,
            browserDebugConsole,
            chunkLiveness);
        var slotArming = new ActiveCaptureSlotArming(segmenterFactory, slotStore, terminalSignal);
        var attemptFlow = new ActiveCaptureAttemptFlow(
            new ActiveCaptureMicGateGuard(micGateState),
            trace,
            slotArming,
            cursorBacklogDrainer,
            chunkLiveness);

        return new ActiveCaptureRuntimeGraph(attemptFlow, chunkFlow, terminalSignal);
    }

    public void Dispose()
    {
        TerminalSignal.DisposeActiveCapture();
        AttemptFlow.Dispose();
    }
}
