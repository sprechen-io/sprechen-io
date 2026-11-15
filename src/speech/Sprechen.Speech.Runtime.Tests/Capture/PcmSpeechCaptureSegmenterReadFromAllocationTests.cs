namespace Sprechen.Speech.Runtime.Tests.Capture;

/// <summary>
/// Prüft Allokationen bei kleinen wiederholten Ringlesevorgängen. Der Zwischenpuffer
/// ist wiederverwendbar, die behaltenen Audioblöcke brauchen weiterhin eine eigene Kopie.
/// Nach Aufwärmaufrufen wird die Allokation des aktuellen Threads gegen ein
/// durchschnittliches Aufrufbudget mit Spielraum für Laufzeitkosten geprüft.
/// </summary>
public sealed class PcmSpeechCaptureSegmenterReadFromAllocationTests
{
    private const int SampleRateHz = InMemorySpeechSampleRing.SampleRateHz;
    private const int BytesPerSample = InMemorySpeechSampleRing.BytesPerSample;
    private const int PartialReadMs = 30;
    private const int NominalReadFrameMs = 20;
    private const int NominalReadFrameBytes = SampleRateHz * BytesPerSample * NominalReadFrameMs / 1000;

    [Fact]
    public void ReadFrom_partial_reads_allocate_only_a_live_frame_sized_chunk()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 30_000);
        var segmenter = NewSegmenter();
        var cursor = ring.OpenCursor(SampleCursorOrigin.Latest);

        var chunkBytes = MillisecondsToBytes(PartialReadMs);
        Assert.True(
            chunkBytes > NominalReadFrameBytes,
            "the written chunk must be larger than one nominal read frame");

        // Ein wiederverwendeter Stillepuffer hält Fixture-Allokationen aus der Messung fern; der Ring kopiert in seinen festen Puffer.
        var silence = MakeSilence(PartialReadMs);

        // Aufwärmaufrufe vermindern erste JIT- und ArrayPool-Kosten im Messfenster.
        for (var i = 0; i < 64; i++)
        {
            ring.WriteSamples(silence);
            segmenter.ReadFrom(ring, ref cursor);
        }

        const int measuredCalls = 256;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < measuredCalls; i++)
        {
            ring.WriteSamples(silence);
            segmenter.ReadFrom(ring, ref cursor);
        }
        var allocatedPerCall = (GC.GetAllocatedBytesForCurrentThread() - before) / measuredCalls;

        // Das Budget von zwei Audioblöcken lässt Laufzeitkosten zu und erkennt größere zusätzliche Pufferallokationen.
        Assert.True(
            allocatedPerCall < NominalReadFrameBytes * 2,
            $"ReadFrom allocated ~{allocatedPerCall} B/call; expected < {NominalReadFrameBytes * 2} B.");
    }

    private static PcmSpeechCaptureSegmenter NewSegmenter() =>
        new(
            mimeType: SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 200,
            captureStartContext: SpeechCaptureStartContext.CleanListening,
            options: new PcmSpeechCaptureSegmenterOptions
            {
                SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                TailSilence = TimeSpan.FromMilliseconds(80),
                MaxInitialSilence = TimeSpan.FromSeconds(30),
            });

    private static int MillisecondsToBytes(int ms) => ms * SampleRateHz * BytesPerSample / 1000;

    private static byte[] MakeSilence(int ms) => new byte[MillisecondsToBytes(ms)];
}
