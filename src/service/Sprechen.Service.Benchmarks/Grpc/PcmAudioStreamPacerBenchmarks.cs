using BenchmarkDotNet.Attributes;
using Sprechen.Service.Grpc;

namespace Sprechen.Service.Benchmarks.Grpc;

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class PcmAudioStreamPacerBenchmarks
{
    private PcmAudioStreamPacerAdapter _pacer = null!;

    [Params(1_024, 4_096)]
    public int BytesSent { get; set; }

    [Params(16_000, 24_000)]
    public int SampleRateHz { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _pacer = new PcmAudioStreamPacerAdapter();
    }

    [Benchmark]
    public Task PaceAsync()
    {
        return _pacer.PaceAsync(BytesSent, SampleRateHz, CancellationToken.None).AsTask();
    }
}
