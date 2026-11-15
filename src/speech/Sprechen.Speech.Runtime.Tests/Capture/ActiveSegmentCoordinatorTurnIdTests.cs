using System.Buffers.Binary;
using Microsoft.Extensions.Options;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class ActiveSegmentCoordinatorTurnIdTests
{
    private const int SampleRateHz = InMemorySpeechSampleRing.SampleRateHz;
    private const int BytesPerSample = InMemorySpeechSampleRing.BytesPerSample;
    private const string ArmedTrace = "speech-capture: capture armed mode=ActivationListening startContext=CleanListening";

    [Fact]
    public async Task CaptureNextSegmentAsync_WhenSpeechCompletes_SegmentCarriesTheTurnIdOfItsArming()
    {
        // Auch ohne Diagnosebericht braucht das Segment die beim Aufnahmebeginn erzeugte Kennung,
        // damit es der folgenden Erkennungsanfrage zugeordnet werden kann.
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        using var coordinator = CreateCoordinator(browserDebugConsole, out var ring);
        coordinator.ResetToLiveEdge();

        var firstCapture = StartCapture(coordinator);
        await browserDebugConsole.WaitForEntryAsync(ArmedTrace, TimeSpan.FromSeconds(1));
        await CompleteCaptureAsync(coordinator, ring);
        var firstSegment = await firstCapture.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(string.IsNullOrWhiteSpace(firstSegment.TurnId));

        var secondCapture = StartCapture(coordinator);
        await WaitForEntryCountAsync(browserDebugConsole, expectedCount: 2);
        await CompleteCaptureAsync(coordinator, ring);
        var secondSegment = await secondCapture.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotEqual(firstSegment.TurnId, secondSegment.TurnId);
    }

    private static Task<CapturedSpeechAudio> StartCapture(ActiveSegmentCoordinator coordinator)
    {
        return coordinator.CaptureNextSegmentAsync(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.ActivationListening),
            CancellationToken.None);
    }

    private static async Task CompleteCaptureAsync(
        ActiveSegmentCoordinator coordinator,
        InMemorySpeechSampleRing ring)
    {
        // Bestätigungsdauer und Stillefrist stammen aus dem Modellprofil;
        // das Testsignal überschreitet beide, unabhängig von den lokalen Testoptionen.
        for (var i = 0; i < 20; i++)
        {
            await ProcessChunkAsync(coordinator, ring, CreatePcmChunk(20, sampleValue: 1000));
        }

        for (var i = 0; i < 80; i++)
        {
            await ProcessChunkAsync(coordinator, ring, CreatePcmChunk(20, sampleValue: 0));
        }
    }

    private static async Task ProcessChunkAsync(
        ActiveSegmentCoordinator coordinator,
        InMemorySpeechSampleRing ring,
        byte[] chunk)
    {
        ring.WriteSamples(chunk);
        await coordinator.ProcessChunkAsync(chunk, CancellationToken.None);
    }

    private static async Task WaitForEntryCountAsync(InMemoryBrowserDebugConsole browserDebugConsole, int expectedCount)
    {
        var deadlineUtc = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1);

        while (DateTimeOffset.UtcNow < deadlineUtc)
        {
            if (browserDebugConsole.Entries.Count(entry => string.Equals(entry, ArmedTrace, StringComparison.Ordinal)) >= expectedCount)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }

        throw new TimeoutException($"Browser debug entry was not observed {expectedCount} times within 1s: {ArmedTrace}");
    }

    private static ActiveSegmentCoordinator CreateCoordinator(
        InMemoryBrowserDebugConsole browserDebugConsole,
        out InMemorySpeechSampleRing ring)
    {
        var optionsFactory = new SpeechCaptureSegmenterOptionsFactory(
            Options.Create(new SpeechCaptureClientOptions()),
            Options.Create(new PcmSpeechCaptureSegmenterOptions
            {
                SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                TailSilence = TimeSpan.FromMilliseconds(20),
                MaxInitialSilence = TimeSpan.FromSeconds(30),
            }));
        var factory = new SpeechCaptureSegmenterFactory(optionsFactory);
        ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        return new ActiveSegmentCoordinator(
            factory,
            ring,
            browserDebugConsole,
            Options.Create(new SpeechCaptureClientOptions
            {
                ChunkArrivalLivenessTimeout = TimeSpan.FromSeconds(30),
            }));
    }

    private static byte[] CreatePcmChunk(int durationMs, short sampleValue)
    {
        var bytes = new byte[durationMs * SampleRateHz * BytesPerSample / 1000];
        for (var i = 0; i < bytes.Length; i += BytesPerSample)
        {
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i, BytesPerSample), sampleValue);
        }

        return bytes;
    }
}
