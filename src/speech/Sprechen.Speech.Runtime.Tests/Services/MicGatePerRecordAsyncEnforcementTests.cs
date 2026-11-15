using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Prüft, dass Capture bei gesperrtem MicGate wartet und durch den CancellationToken abgebrochen
/// werden kann. Ein offenes Gate wird ebenfalls mit anschließendem Abbruch geprüft.
/// </summary>
public sealed class MicGatePerRecordAsyncEnforcementTests
{
    [Fact]
    public async Task CaptureAsync_WhileTheAssistantSpeaks_WaitsInsteadOfEndingTheTurn()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var optionsFactory = new SpeechCaptureSegmenterOptionsFactory(
            Options.Create(new SpeechCaptureClientOptions()),
            Options.Create(new PcmSpeechCaptureSegmenterOptions
            {
                SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                TailSilence = TimeSpan.FromMilliseconds(80),
                MaxInitialSilence = TimeSpan.FromSeconds(10),
            }));
        var segmenterFactory = new SpeechCaptureSegmenterFactory(optionsFactory);
        var hardOffGate = new StubMicGateState(isHardOff: true);
        var coordinator = new SpeechCaptureActiveCaptureCoordinator(
            new NoOpUserSpeechBargeInHandler(),
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            new NoOpBrowserDebugConsole(),
            segmenterFactory,
            ring,
            hardOffGate);
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);
        using var cts = new CancellationTokenSource();

        var capture = coordinator.CaptureAsync(
            mimeType: SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 1_000,
            captureStartContext: SpeechCaptureStartContext.CleanListening,
            behavior: behavior,
            initialSilenceTimeout: null,
            cancellationToken: cts.Token);

        await Task.Delay(50);
        Assert.False(capture.IsCompleted, "a speaking assistant must not end the turn");

        // Die Freigabe nach dem Wiedergabeende simulieren.
        hardOffGate.Open();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => capture);
    }

    [Fact]
    public async Task CaptureAsync_WhenTheTurnIsCancelledWhileSpeaking_DoesNotHang()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var optionsFactory = new SpeechCaptureSegmenterOptionsFactory(
            Options.Create(new SpeechCaptureClientOptions()),
            Options.Create(new PcmSpeechCaptureSegmenterOptions
            {
                SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                TailSilence = TimeSpan.FromMilliseconds(80),
                MaxInitialSilence = TimeSpan.FromSeconds(10),
            }));
        var segmenterFactory = new SpeechCaptureSegmenterFactory(optionsFactory);
        var coordinator = new SpeechCaptureActiveCaptureCoordinator(
            new NoOpUserSpeechBargeInHandler(),
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            new NoOpBrowserDebugConsole(),
            segmenterFactory,
            ring,
            new StubMicGateState(isHardOff: true));
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);
        using var cts = new CancellationTokenSource();

        var capture = coordinator.CaptureAsync(
            mimeType: SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 1_000,
            captureStartContext: SpeechCaptureStartContext.CleanListening,
            behavior: behavior,
            initialSilenceTimeout: null,
            cancellationToken: cts.Token);

        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => capture.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task CaptureAsync_WhenMicGateIsNotHardOff_ProceedsNormally()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var optionsFactory = new SpeechCaptureSegmenterOptionsFactory(
            Options.Create(new SpeechCaptureClientOptions()),
            Options.Create(new PcmSpeechCaptureSegmenterOptions
            {
                SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                TailSilence = TimeSpan.FromMilliseconds(80),
                MaxInitialSilence = TimeSpan.FromSeconds(10),
            }));
        var segmenterFactory = new SpeechCaptureSegmenterFactory(optionsFactory);
        var coordinator = new SpeechCaptureActiveCaptureCoordinator(
            new NoOpUserSpeechBargeInHandler(),
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            new NoOpBrowserDebugConsole(),
            segmenterFactory,
            ring,
            new StubMicGateState(isHardOff: false));
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);

        using var cts = new CancellationTokenSource();
        var captureTask = coordinator.CaptureAsync(
            mimeType: SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 1_000,
            captureStartContext: SpeechCaptureStartContext.CleanListening,
            behavior: behavior,
            initialSilenceTimeout: null,
            cancellationToken: cts.Token);

        // Dem Coordinator kurz Zeit geben und den sonst weiterlaufenden Capture anschließend abbrechen.
        await Task.Delay(50);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => captureTask);
    }
}
