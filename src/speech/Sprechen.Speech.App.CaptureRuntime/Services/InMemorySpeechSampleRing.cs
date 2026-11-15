namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Sperrgeschützte Umsetzung von <see cref="SpeechSampleRing"/> im Arbeitsspeicher.
/// Reserviert einen zusammenhängenden <see cref="byte"/>-Puffer für die konfigurierte
/// Vorlaufdauer bei 16 kHz und zwei Bytes pro Sample, damit Schreibvorgänge keinen
/// neuen Puffer anlegen müssen.
/// </summary>
/// <remarks>
/// Ein <see cref="object"/>-Monitor schützt Schreibposition und Puffer beim Lesen und Schreiben.
/// Die als <c>Scoped</c> registrierte Instanz wird innerhalb einer Sitzung gemeinsam verwendet.
/// </remarks>
public sealed class InMemorySpeechSampleRing : SpeechSampleRing
{
    public const int SampleRateHz = 16_000;
    public const int BytesPerSample = 2;
    public const int DefaultMaxLookbackMs = 30_000;

    private readonly byte[] _buffer;
    private readonly object _gate = new();
    private long _writeByteIndex;

    public InMemorySpeechSampleRing()
        : this(DefaultMaxLookbackMs)
    {
    }

    public InMemorySpeechSampleRing(int maxLookbackMs)
    {
        if (maxLookbackMs <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxLookbackMs),
                maxLookbackMs,
                "Ring lookback window must be positive.");
        }

        var byteCapacityLong = (long)maxLookbackMs * SampleRateHz * BytesPerSample / 1000L;
        if (byteCapacityLong <= 0 || byteCapacityLong > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxLookbackMs),
                maxLookbackMs,
                "Ring lookback window translates to an unsupported byte capacity.");
        }

        MaxLookbackMs = maxLookbackMs;
        _buffer = new byte[(int)byteCapacityLong];
    }

    /// <summary>
    /// Konfiguriertes Rückblickfenster in Millisekunden. Entspricht
    /// <see cref="Capacity"/> geteilt durch (16 kHz mal 2 B) mal 1000.
    /// </summary>
    public int MaxLookbackMs { get; }

    /// <summary>
    /// Vorab reservierte Ringkapazität in Bytes, ohne Nachallokation im Schreibpfad.
    /// </summary>
    public int Capacity => _buffer.Length;

    /// <summary>
    /// Summe der seit der Erstellung in den Ring geschriebenen Bytes.
    /// Vor allem für Tests und Telemetrie. Abnehmer sollen
    /// <see cref="OpenCursor"/> nutzen statt dieser Zählung.
    /// </summary>
    public long WriteByteIndex
    {
        get
        {
            lock (_gate)
            {
                return _writeByteIndex;
            }
        }
    }

    public override void WriteSamples(ReadOnlySpan<byte> samples)
    {
        if (samples.IsEmpty)
        {
            return;
        }

        lock (_gate)
        {
            var remaining = samples;

            // Bei einem größeren Schreibvorgang bleiben nur die neuesten Bytes bis zur Ringkapazität erhalten.
            // Der Schreibindex zählt auch die verworfenen Bytes, damit bestehende Cursor
            // weiterhin die richtige absolute Position bezeichnen.
            if (remaining.Length > _buffer.Length)
            {
                var overflow = remaining.Length - _buffer.Length;
                _writeByteIndex += overflow;
                remaining = remaining.Slice(overflow);
            }

            var pos = (int)(_writeByteIndex % _buffer.Length);
            var first = Math.Min(remaining.Length, _buffer.Length - pos);
            remaining.Slice(0, first).CopyTo(_buffer.AsSpan(pos));

            if (first < remaining.Length)
            {
                remaining.Slice(first).CopyTo(_buffer.AsSpan(0));
            }

            _writeByteIndex += remaining.Length;
        }
    }

    public override SampleCursor OpenCursor(SampleCursorOrigin origin)
    {
        lock (_gate)
        {
            return origin switch
            {
                SampleCursorOrigin.Latest => new SampleCursor(_writeByteIndex),
                SampleCursorOrigin.Earliest => new SampleCursor(EarliestRetainedIndexLocked()),
                _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, "Unknown cursor origin."),
            };
        }
    }

    public override int Read(SampleCursor cursor, Span<byte> destination)
    {
        if (destination.IsEmpty)
        {
            // Einen leeren Zielpuffer als Aufruffehler melden statt als fehlende Audiodaten.
            throw new ArgumentException(
                "Ring reads must request at least one byte; zero-length reads are not permitted.",
                nameof(destination));
        }

        lock (_gate)
        {
            var earliest = EarliestRetainedIndexLocked();
            if (cursor.SampleIndex < earliest)
            {
                throw new RingCursorStaleException(cursor.SampleIndex, earliest);
            }

            var available = _writeByteIndex - cursor.SampleIndex;
            if (available <= 0)
            {
                return 0;
            }

            var toRead = (int)Math.Min(available, destination.Length);
            var pos = (int)(cursor.SampleIndex % _buffer.Length);
            var first = Math.Min(toRead, _buffer.Length - pos);

            _buffer.AsSpan(pos, first).CopyTo(destination);

            if (first < toRead)
            {
                _buffer.AsSpan(0, toRead - first).CopyTo(destination.Slice(first));
            }

            return toRead;
        }
    }

    private long EarliestRetainedIndexLocked()
    {
        return _writeByteIndex <= _buffer.Length
            ? 0L
            : _writeByteIndex - _buffer.Length;
    }
}
