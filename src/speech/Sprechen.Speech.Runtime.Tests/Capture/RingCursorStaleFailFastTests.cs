namespace Sprechen.Speech.Runtime.Tests.Capture;

/// <summary>
/// Überschreibt den Rückblick des Rings und prüft beim Lesen des alten Cursors
/// RingCursorStaleException mit dem Code RING_CURSOR_STALE. Der Datenverlust
/// muss ausdrücklich sichtbar werden, damit der Aufrufer über die Fortsetzung entscheiden kann.
/// </summary>
public sealed class RingCursorStaleFailFastTests
{
    [Fact]
    public void Read_after_writer_advanced_past_maxLookback_must_throw_RingCursorStaleException()
    {
        // Ein kurzer Rückblick macht den Überlauf schnell und eindeutig.
        const int maxLookbackMs = 50;
        var ring = new InMemorySpeechSampleRing(maxLookbackMs);

        var staleCursor = ring.OpenCursor(SampleCursorOrigin.Earliest);
        Assert.Equal(0, staleCursor.SampleIndex);

        var overflow = new byte[ring.Capacity * 2];
        for (var i = 0; i < overflow.Length; i++)
        {
            overflow[i] = (byte)(i & 0xFF);
        }

        ring.WriteSamples(overflow);

        var buffer = new byte[16];
        var ex = Assert.Throws<RingCursorStaleException>(() => ring.Read(staleCursor, buffer));

        Assert.Equal("RING_CURSOR_STALE", ex.Code);
        Assert.Equal(staleCursor.SampleIndex, ex.RequestedSampleIndex);
        Assert.True(
            ex.EarliestRetainedSampleIndex > staleCursor.SampleIndex,
            "earliest retained sample must have advanced past the stale cursor");
    }
}
