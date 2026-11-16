namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Prüft Kapazität, Umlauf und unabhängige Lesecursor von <see cref="InMemorySpeechSampleRing"/>
/// sowie die Ablehnung überholter Cursor und leerer Lesepuffer.
/// </summary>
public sealed class InMemorySpeechSampleRingTests
{
    private const int SampleRateHz = InMemorySpeechSampleRing.SampleRateHz;
    private const int BytesPerSample = InMemorySpeechSampleRing.BytesPerSample;

    [Fact]
    public void Constructor_rejects_non_positive_lookback()
    {
        var zero = Assert.Throws<ArgumentOutOfRangeException>(() => new InMemorySpeechSampleRing(0));
        var negative = Assert.Throws<ArgumentOutOfRangeException>(() => new InMemorySpeechSampleRing(-5));
        var overflow = Assert.Throws<ArgumentOutOfRangeException>(() => new InMemorySpeechSampleRing(67_108_864));

        Assert.Equal("maxLookbackMs", zero.ParamName);
        Assert.Equal("maxLookbackMs", negative.ParamName);
        Assert.Equal("maxLookbackMs", overflow.ParamName);
    }

    [Fact]
    public void Capacity_matches_configured_lookback()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 100);

        Assert.Equal(100 * SampleRateHz * BytesPerSample / 1000, ring.Capacity);
    }

    [Fact]
    public void Read_returns_bytes_written_in_order_when_no_wrap()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 1_000);
        var written = MakePattern(800);

        ring.WriteSamples(written);

        var cursor = new SampleCursor(0);
        var buffer = new byte[written.Length];

        var bytesRead = ring.Read(cursor, buffer);

        Assert.Equal(written.Length, bytesRead);
        Assert.Equal(written, buffer);
    }

    [Fact]
    public void Read_returns_zero_when_caller_is_caught_up_with_writer()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 100);
        ring.WriteSamples(MakePattern(64));

        var cursor = ring.OpenCursor(SampleCursorOrigin.Latest);
        var buffer = new byte[32];

        Assert.Equal(0, ring.Read(cursor, buffer));
    }

    [Fact]
    public void Read_recovers_correct_bytes_after_wrap_around()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 50);
        var capacity = ring.Capacity;
        Assert.Equal(50 * SampleRateHz * BytesPerSample / 1000, capacity);

        // Drei halbe Pufferfüllungen überschreiben den ersten Block; die letzten beiden bleiben erhalten.
        var window = MakePattern(capacity / 2, seed: 0);
        ring.WriteSamples(window);
        var window2 = MakePattern(capacity / 2, seed: 1000);
        ring.WriteSamples(window2);
        var window3 = MakePattern(capacity / 2, seed: 2000);
        ring.WriteSamples(window3);

        var totalWritten = (long)window.Length + window2.Length + window3.Length;
        Assert.Equal(totalWritten, ring.WriteByteIndex);

        var earliestCursor = ring.OpenCursor(SampleCursorOrigin.Earliest);
        Assert.Equal(totalWritten - capacity, earliestCursor.SampleIndex);

        var buffer = new byte[capacity];

        var bytesRead = ring.Read(earliestCursor, buffer);

        Assert.Equal(capacity, bytesRead);

        var expected = new byte[capacity];
        window2.CopyTo(expected, 0);
        window3.CopyTo(expected, window2.Length);
        Assert.Equal(expected, buffer);
    }

    [Fact]
    public void Read_throws_RingCursorStaleException_when_writer_advances_past_cursor()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 50);
        var staleCursor = ring.OpenCursor(SampleCursorOrigin.Earliest);

        // Zwei volle Pufferfüllungen verdrängen die Daten am ursprünglichen Cursor sicher.
        var overflow = new byte[ring.Capacity * 2];
        ring.WriteSamples(overflow);

        var buffer = new byte[16];

        var ex = Assert.Throws<RingCursorStaleException>(() => ring.Read(staleCursor, buffer));
        Assert.Equal("RING_CURSOR_STALE", ex.Code);
        Assert.Equal(staleCursor.SampleIndex, ex.RequestedSampleIndex);
        Assert.True(ex.EarliestRetainedSampleIndex > staleCursor.SampleIndex);
    }

    [Fact]
    public void Read_throws_when_destination_is_empty()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 100);
        ring.WriteSamples(MakePattern(32));
        var cursor = new SampleCursor(0);

        Assert.Throws<ArgumentException>(() => ring.Read(cursor, Span<byte>.Empty));
    }

    [Fact]
    public void Single_writer_multi_reader_each_reader_observes_full_stream()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 1_000);
        var readerA = ring.OpenCursor(SampleCursorOrigin.Earliest);
        var readerB = ring.OpenCursor(SampleCursorOrigin.Earliest);

        var samples = MakePattern(2_048);
        ring.WriteSamples(samples);

        var bufferA = new byte[samples.Length];
        var bufferB = new byte[samples.Length];

        var readA = ring.Read(readerA, bufferA);
        var readB = ring.Read(readerB, bufferB);

        Assert.Equal(samples.Length, readA);
        Assert.Equal(samples.Length, readB);
        Assert.Equal(samples, bufferA);
        Assert.Equal(samples, bufferB);
    }

    [Fact]
    public void WriteSamples_larger_than_capacity_keeps_only_trailing_capacity_bytes()
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 50);
        var huge = MakePattern(ring.Capacity * 3);

        ring.WriteSamples(huge);

        var earliest = ring.OpenCursor(SampleCursorOrigin.Earliest);
        Assert.Equal(huge.Length - ring.Capacity, earliest.SampleIndex);

        var buffer = new byte[ring.Capacity];

        var bytesRead = ring.Read(earliest, buffer);

        Assert.Equal(ring.Capacity, bytesRead);
        var expected = huge.AsSpan(huge.Length - ring.Capacity).ToArray();
        Assert.Equal(expected, buffer);
    }

    private static byte[] MakePattern(int byteCount, int seed = 0)
    {
        var data = new byte[byteCount];
        for (var i = 0; i < byteCount; i++)
        {
            data[i] = (byte)((i + seed) & 0xFF);
        }

        return data;
    }
}
