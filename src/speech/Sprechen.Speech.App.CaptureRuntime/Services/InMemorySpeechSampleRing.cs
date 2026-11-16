using MicroSharp;
using MicroSharp.Processing;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// In-memory <see cref="SpeechSampleRing"/> over a fixed <see cref="PcmHistoryRing"/>.
/// The window is 16 kHz signed 16-bit mono. Byte offsets stay on <see cref="SampleCursor.SampleIndex"/>.
/// </summary>
/// <remarks>
/// One writer and any number of readers share the history lock inside the package.
/// A cursor past the write position returns 0, matching the previous application contract.
/// A cursor behind the retained window becomes <see cref="RingCursorStaleException"/>.
/// </remarks>
public sealed class InMemorySpeechSampleRing : SpeechSampleRing
{
    public const int SampleRateHz = 16_000;
    public const int BytesPerSample = 2;
    public const int DefaultMaxLookbackMs = 30_000;

    private readonly PcmHistoryRing _ring;

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
        _ring = new PcmHistoryRing(PcmFormat.Default, TimeSpan.FromMilliseconds(maxLookbackMs));
        if (_ring.CapacityBytes != byteCapacityLong)
        {
            throw new InvalidOperationException("History capacity does not match the 16 kHz lookback window.");
        }
    }

    /// <summary>Configured lookback in milliseconds at 16 kHz and two bytes per sample.</summary>
    public int MaxLookbackMs { get; }

    /// <summary>Fixed history capacity in bytes.</summary>
    public int Capacity => _ring.CapacityBytes;

    /// <summary>Absolute count of bytes accepted by <see cref="WriteSamples"/>, including bytes that fell out of the window.</summary>
    public long WriteByteIndex => _ring.WriteBytePosition;

    public override void WriteSamples(ReadOnlySpan<byte> samples) => _ring.Write(samples);

    public override SampleCursor OpenCursor(SampleCursorOrigin origin)
    {
        var historyOrigin = origin switch
        {
            SampleCursorOrigin.Latest => PcmHistoryCursorOrigin.Latest,
            SampleCursorOrigin.Earliest => PcmHistoryCursorOrigin.Earliest,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, "Unknown cursor origin."),
        };
        return new SampleCursor(_ring.OpenCursor(historyOrigin).BytePosition);
    }

    public override int Read(SampleCursor cursor, Span<byte> destination)
    {
        if (destination.IsEmpty)
        {
            throw new ArgumentException(
                "Ring reads must request at least one byte; zero-length reads are not permitted.",
                nameof(destination));
        }

        try
        {
            return _ring.Read(new PcmHistoryCursor(cursor.SampleIndex), destination);
        }
        catch (PcmHistoryCursorStaleException exception)
        {
            throw new RingCursorStaleException(exception.RequestedBytePosition, exception.EarliestRetainedBytePosition);
        }
        catch (PcmHistoryCursorInvalidException)
        {
            return 0;
        }
    }
}
