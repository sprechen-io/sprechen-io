using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sprechen.Speech.Runtime.Tests.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Capture;

/// <summary>
/// Prüft Drain und Abort am Aufnahmekoordinator. Drain gibt nach Sprachbeginn
/// bisher erfasstes Audio zurück; Abort bricht die wartende Aufnahmeaufgabe ab.
/// Der Aufrufer kann dadurch ein ausdrücklich gewünschtes Teilergebnis erhalten.
/// </summary>
public sealed class SpeechCaptureDrainCancellationTests
{
    private const int SampleRateHz = InMemorySpeechSampleRing.SampleRateHz;
    private const int BytesPerSample = InMemorySpeechSampleRing.BytesPerSample;

    [Fact]
    public async Task CancelActiveCapture_with_Drain_emits_partial_CapturedSpeechAudio()
    {
        var (coordinator, ring) = CreateCoordinator();
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);

        var captureTask = coordinator.CaptureAsync(
            mimeType: SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 1_000,
            captureStartContext: SpeechCaptureStartContext.CleanListening,
            behavior: behavior,
            initialSilenceTimeout: null,
            cancellationToken: CancellationToken.None);

        // Die Aufnahme erhält vor der Audiozufuhr Gelegenheit, die Sperre zu passieren und den Segmentierer zu aktivieren.
        await Task.Yield();
        await WaitForArmedAsync(coordinator);

        // Genügend synthetische Sprache liefern, um einen bestätigten Beginn und gepuffertes Audio zu erhalten.
        for (var i = 0; i < 6; i++)
        {
            await ProcessChunkAsync(coordinator, ring, MakeSpeech(64));
        }

        coordinator.CancelActiveCapture(CaptureCancellationKind.Drain, CancellationToken.None);

        var captured = await captureTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotNull(captured);
        Assert.NotEmpty(captured.Chunks);
        Assert.True(
            captured.Chunks.Sum(c => c.Length) > 0,
            "Drain must emit a non-empty partial CapturedSpeechAudio (TrySetResult), not collapse via TrySetCanceled.");
        Assert.Equal(SpeechAudioMimeType.Pcm, captured.MimeType);
    }

    [Fact]
    public async Task CancelActiveCapture_with_Abort_keeps_TrySetCanceled_behavior()
    {
        var (coordinator, ring) = CreateCoordinator();
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);

        var captureTask = coordinator.CaptureAsync(
            mimeType: SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 1_000,
            captureStartContext: SpeechCaptureStartContext.CleanListening,
            behavior: behavior,
            initialSilenceTimeout: null,
            cancellationToken: CancellationToken.None);

        await Task.Yield();
        await WaitForArmedAsync(coordinator);

        for (var i = 0; i < 6; i++)
        {
            await ProcessChunkAsync(coordinator, ring, MakeSpeech(64));
        }

        using var abortCts = new CancellationTokenSource();
        abortCts.Cancel();

        coordinator.CancelActiveCapture(CaptureCancellationKind.Abort, abortCts.Token);

        await Assert.ThrowsAsync<TaskCanceledException>(() => captureTask.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    private static (SpeechCaptureActiveCaptureCoordinator coordinator, InMemorySpeechSampleRing ring) CreateCoordinator()
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
        return (coordinator, ring);
    }

    private static async Task WaitForArmedAsync(SpeechCaptureActiveCaptureCoordinator coordinator)
    {
        // Ohne beobachtbares Aktivierungssignal wartet der Test kurz vor der Audiozufuhr.
        // Diese Verzögerung bestätigt den tatsächlichen Aufnahmezustand nicht.
        for (var i = 0; i < 50; i++)
        {
            await Task.Delay(2);
        }
    }

    private static async Task ProcessChunkAsync(
        SpeechCaptureActiveCaptureCoordinator coordinator,
        InMemorySpeechSampleRing ring,
        byte[] chunk)
    {
        ring.WriteSamples(chunk);
        await coordinator.ProcessChunkAsync(chunk, CancellationToken.None);
    }

    private static byte[] MakeSpeech(int ms)
    {
        var bytes = new byte[ms * SampleRateHz * BytesPerSample / 1000];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = 0x55;
        }

        return bytes;
    }
}
