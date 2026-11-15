namespace Sprechen.Speech.Runtime.Tests.Capture;

/// <summary>
/// Modelliert Audio zwischen zwei Aufnahmefenstern während einer ausstehenden Erkennung.
/// Nach einer synthetischen Äußerung wird ein Cursor gespeichert und vor dem nächsten Lesen
/// weiteres Audio für 100 ms geschrieben. Geprüft wird dessen bytegenauer Erhalt im Ring;
/// Erkennung und Aufnahmekoordinator werden nicht ausgeführt.
/// </summary>
public sealed class SpeechCaptureRoundtripGapTests
{
    private const int SampleRateHz = InMemorySpeechSampleRing.SampleRateHz;
    private const int BytesPerSample = InMemorySpeechSampleRing.BytesPerSample;

    [Fact]
    public void NextUtterance_AfterSegmentBoundary_ContainsSpeechThatArrivedDuringSttRoundtrip()
    {
        const int interSegmentSpeechMs = 100;
        var expectedInterSegmentSpeechBytes = MillisecondsToBytes(interSegmentSpeechMs);
        Assert.True(expectedInterSegmentSpeechBytes > 0);

        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);

        ring.WriteSamples(MakeSilence(200));
        ring.WriteSamples(MakeSpeech(100, marker: 0x3C));
        ring.WriteSamples(MakeSilence(200));

        var boundaryCursor = ring.OpenCursor(SampleCursorOrigin.Latest);

        var interSegmentSpeech = MakeSpeech(interSegmentSpeechMs, marker: 0x77);
        ring.WriteSamples(interSegmentSpeech);

        var buffer = new byte[expectedInterSegmentSpeechBytes];
        var bytesRead = ring.Read(boundaryCursor, buffer);

        Assert.Equal(expectedInterSegmentSpeechBytes, bytesRead);
        Assert.Equal(interSegmentSpeech, buffer);

        Assert.Contains(buffer, b => b != 0);
    }

    private static int MillisecondsToBytes(int ms) =>
        ms * SampleRateHz * BytesPerSample / 1000;

    private static byte[] MakeSilence(int ms) => new byte[MillisecondsToBytes(ms)];

    private static byte[] MakeSpeech(int ms, byte marker)
    {
        var bytes = new byte[MillisecondsToBytes(ms)];
        for (var i = 0; i < bytes.Length; i++)
        {
            // Ein festes Muster ungleich null unterscheidet das synthetische Sprachfenster von der umgebenden Stille.
            bytes[i] = (byte)(marker ^ (i & 0x7F) | 0x01);
        }

        return bytes;
    }
}
