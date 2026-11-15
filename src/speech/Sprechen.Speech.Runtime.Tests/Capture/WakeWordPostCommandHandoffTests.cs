namespace Sprechen.Speech.Runtime.Tests.Capture;

/// <summary>
/// Prüft die Cursorübergabe zwischen Ring und Segmentierer. Nach dem Cursor gespeichertes
/// Audio wird mit ReadFrom gelesen. Der Test verlangt einen erfolgreichen Segmentabschluss
/// und mindestens 960 Bytes, entsprechend der simulierten Lücke von 30 ms. Einzelne Bytes
/// der Lücke werden nicht verglichen; ein Aktivierungsablauf wird nicht ausgeführt.
/// </summary>
public sealed class WakeWordPostCommandHandoffTests
{
    private const int SampleRateHz = InMemorySpeechSampleRing.SampleRateHz;
    private const int BytesPerSample = InMemorySpeechSampleRing.BytesPerSample;
    private const int HandoffPauseMs = 30;

    [Fact]
    public void PostWakeCommandCapture_DuringTheHandoffPause_IncludesTheArrivingAudio()
    {
        var minHandoffBytes = MillisecondsToBytes(HandoffPauseMs);
        Assert.Equal(960, minHandoffBytes);

        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var segmenter = new PcmSpeechCaptureSegmenter(
            mimeType: SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 200,
            captureStartContext: SpeechCaptureStartContext.CleanListening,
            options: new PcmSpeechCaptureSegmenterOptions
            {
                SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                TailSilence = TimeSpan.FromMilliseconds(80),
                MaxInitialSilence = TimeSpan.FromSeconds(5),
            });

        ring.WriteSamples(MakeSpeech(200));
        var handoffCursor = ring.OpenCursor(SampleCursorOrigin.Latest);
        var cursor = handoffCursor;

        ring.WriteSamples(MakeSpeech(HandoffPauseMs));
        var gapUpdate = segmenter.ReadFrom(ring, ref cursor);
        Assert.Null(gapUpdate.CompletionException);
        Assert.Null(gapUpdate.CompletedUtterance);

        // Schreiben und Lesen wechseln sich ab, damit jeder gelesene Block nur synthetische
        // Sprache oder Stille enthält. Gemischte Blöcke würden über einen gemeinsamen Pegel gemittelt.
        ring.WriteSamples(MakeSpeech(80));
        ReadAll(segmenter, ring, ref cursor);
        ring.WriteSamples(MakeSpeech(80));
        ReadAll(segmenter, ring, ref cursor);
        ring.WriteSamples(MakeSpeech(80));
        ReadAll(segmenter, ring, ref cursor);

        ring.WriteSamples(MakeSilence(300));

        CapturedSpeechAudio? captured = null;
        for (var attempts = 0; attempts < 32; attempts++)
        {
            var before = cursor.SampleIndex;
            var update = segmenter.ReadFrom(ring, ref cursor);
            Assert.Null(update.CompletionException);
            if (update.CompletedUtterance is { } u)
            {
                captured = u;
                break;
            }

            if (cursor.SampleIndex == before)
            {
                break;
            }
        }

        Assert.NotNull(captured);
        var totalBytes = captured!.Chunks.Sum(c => c.Length);
        Assert.True(
            totalBytes >= minHandoffBytes,
            $"Expected captured audio total >= {minHandoffBytes} bytes ({HandoffPauseMs} ms x {SampleRateHz} Hz x {BytesPerSample} B); got {totalBytes}.");
    }

    private static void ReadAll(
        PcmSpeechCaptureSegmenter segmenter,
        SpeechSampleRing ring,
        ref SampleCursor cursor)
    {
        for (var attempts = 0; attempts < 16; attempts++)
        {
            var before = cursor.SampleIndex;
            var update = segmenter.ReadFrom(ring, ref cursor);
            Assert.Null(update.CompletionException);
            if (cursor.SampleIndex == before)
            {
                return;
            }
        }
    }

    private static int MillisecondsToBytes(int ms) =>
        ms * SampleRateHz * BytesPerSample / 1000;

    private static byte[] MakeSilence(int ms) => new byte[MillisecondsToBytes(ms)];

    private static byte[] MakeSpeech(int ms)
    {
        // PCM16LE mit Samplewert 0x5555 entspricht 21845 und liegt deutlich über der Sprachschwelle 350.
        var bytes = new byte[MillisecondsToBytes(ms)];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = 0x55;
        }

        return bytes;
    }
}
