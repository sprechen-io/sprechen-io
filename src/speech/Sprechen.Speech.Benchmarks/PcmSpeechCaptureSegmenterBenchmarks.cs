using BenchmarkDotNet.Attributes;
using Sprechen.Speech.App.CaptureRuntime.Services;

namespace Sprechen.Speech.Benchmarks;

[MemoryDiagnoser]
public class PcmSpeechCaptureSegmenterBenchmarks
{
    private static readonly PcmSpeechCaptureSegmenterOptions SegmenterOptions = new();
    private const int SampleRateHz = 16000;
    private const int BytesPerSample = 2;
    private const int ChunkMs = 20;

    private byte[] _silenceChunk = [];
    private byte[] _speechChunk = [];
    private byte[][] _typicalUtterance = [];

    [Params(960, 3000)]
    public int SilenceDebounceMs { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _silenceChunk = CreateSilenceChunkMs(ChunkMs);
        _speechChunk = CreateSpeechChunkMs(ChunkMs);

        var chunks = new List<byte[]>();

        // 100 ms synthetischer Vorlauf.
        for (var i = 0; i < 5; i++)
        {
            chunks.Add(_silenceChunk);
        }

        // 1 s synthetisches Sprachsignal.
        for (var i = 0; i < 50; i++)
        {
            chunks.Add(_speechChunk);
        }

        // Stille für die eingestellte Abschlussfrist plus 100 ms.
        var silenceChunks = (SilenceDebounceMs / ChunkMs) + 5;
        for (var i = 0; i < silenceChunks; i++)
        {
            chunks.Add(_silenceChunk);
        }

        _typicalUtterance = chunks.ToArray();
    }

    [Benchmark]
    public int SegmentTypicalUtterance()
    {
        return PcmSpeechCaptureSegmentation.SegmentChunkSequence(_typicalUtterance, SilenceDebounceMs, SegmenterOptions);
    }

    private static byte[] CreateSilenceChunkMs(int durationMs)
    {
        var samples = durationMs * SampleRateHz / 1000;
        return new byte[samples * BytesPerSample];
    }

    private static byte[] CreateSpeechChunkMs(int durationMs)
    {
        var samples = durationMs * SampleRateHz / 1000;
        var buffer = new byte[samples * BytesPerSample];

        for (var i = 0; i < samples; i++)
        {
            var offset = i * BytesPerSample;
            buffer[offset] = 0xE8; // Samplewert 1000 in Little-Endian-Bytefolge.
            buffer[offset + 1] = 0x03;
        }

        return buffer;
    }
}

