using BenchmarkDotNet.Attributes;
using Sprechen.Speech.App.CaptureRuntime.Services;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Benchmarks;

/// <summary>
/// Misst Ring und Segmentierer unter CoreCLR. Die Sitzungspumpe ist hier wegen ihrer
/// internen Sichtbarkeit nicht enthalten; den vollständigen Verarbeitungspfad prüft
/// SpeechCaptureChunkPathAllocationTests im Testprojekt.
/// </summary>
[MemoryDiagnoser]
public class SpeechCaptureChunkPathBenchmarks
{
    private const int SampleRateHz = InMemorySpeechSampleRing.SampleRateHz;
    private const int BytesPerSample = InMemorySpeechSampleRing.BytesPerSample;
    private const int ChunkMs = 20;
    private const int ChunkBytes = SampleRateHz * BytesPerSample * ChunkMs / 1000;

    private InMemorySpeechSampleRing _ring = null!;
    private SampleCursor _cursor;
    private byte[] _chunk = [];
    private byte[] _readBuffer = [];
    private byte[][] _preRollSilence = [];
    private byte[][] _confirmedSpeech = [];
    private readonly PcmSpeechCaptureSegmenterOptions _options = new();

    [GlobalSetup]
    public void Setup()
    {
        _ring = new InMemorySpeechSampleRing(maxLookbackMs: 30_000);
        _chunk = CreateSpeechChunk();
        _readBuffer = new byte[ChunkBytes];
        _ring.WriteSamples(_chunk);
        _cursor = _ring.OpenCursor(SampleCursorOrigin.Earliest);

        _preRollSilence = Enumerable.Repeat(new byte[ChunkBytes], 10).ToArray();
        var speech = new List<byte[]>();
        for (var i = 0; i < 5; i++)
        {
            speech.Add(new byte[ChunkBytes]);
        }

        for (var i = 0; i < 50; i++)
        {
            speech.Add(CreateSpeechChunk());
        }

        for (var i = 0; i < 53; i++)
        {
            speech.Add(new byte[ChunkBytes]);
        }

        _confirmedSpeech = speech.ToArray();
    }

    [Benchmark]
    public int RingWriteThenReadOneChunk()
    {
        _ring.WriteSamples(_chunk);
        _cursor = _ring.OpenCursor(SampleCursorOrigin.Latest);
        _cursor = new SampleCursor(_cursor.SampleIndex - ChunkBytes);
        return _ring.Read(_cursor, _readBuffer);
    }

    [Benchmark]
    public int SegmenterPreRollSilence()
    {
        return PcmSpeechCaptureSegmentation.SegmentChunkSequence(_preRollSilence, silenceDebounceMs: 960, _options);
    }

    [Benchmark]
    public int SegmenterConfirmedSpeech()
    {
        return PcmSpeechCaptureSegmentation.SegmentChunkSequence(_confirmedSpeech, silenceDebounceMs: 960, _options);
    }

    private static byte[] CreateSpeechChunk()
    {
        var buffer = new byte[ChunkBytes];
        for (var i = 0; i < buffer.Length; i += 2)
        {
            buffer[i] = 0xE8;
            buffer[i + 1] = 0x03;
        }

        return buffer;
    }
}
