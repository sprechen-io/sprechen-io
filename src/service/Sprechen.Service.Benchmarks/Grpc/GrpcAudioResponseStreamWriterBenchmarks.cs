using BenchmarkDotNet.Attributes;
using Sprechen.Service.Benchmarks.TestDoubles;
using Sprechen.Service.Core.Services;
using Sprechen.Service.Grpc;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Benchmarks.Grpc;

[MemoryDiagnoser]
public class GrpcAudioResponseStreamWriterBenchmarks
{
    private NullServerStreamWriter<SprechenProto.AudioResponse> _sink = null!;
    private GrpcAudioResponseStreamWriterAdapter _writer = null!;
    private AudioSynthesisChunk _pcmChunk;
    private AudioSynthesisChunk _mp3Chunk;

    [Params(4_096, 16_384, 65_536, 262_144)]
    public int DataBytes { get; set; }

    [Params(16_000, 24_000)]
    public int PcmSampleRateHz { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _sink = new NullServerStreamWriter<SprechenProto.AudioResponse>();
        _writer = new GrpcAudioResponseStreamWriterAdapter(pacePcm: false);

        _pcmChunk = new AudioSynthesisChunk(CreatePayload(DataBytes), AudioMimeType.Pcm, PcmSampleRateHz);
        _mp3Chunk = new AudioSynthesisChunk(CreatePayload(DataBytes), AudioMimeType.Mp3);
    }

    [Benchmark(Baseline = true)]
    public Task WritePcm()
        => _writer.WriteAsync(_sink, _pcmChunk, CancellationToken.None);

    [Benchmark]
    public Task WriteMp3()
        => _writer.WriteAsync(_sink, _mp3Chunk, CancellationToken.None);

    private static byte[] CreatePayload(int length)
    {
        if (length <= 0)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[length];
        Random.Shared.NextBytes(buffer);
        return buffer;
    }
}
