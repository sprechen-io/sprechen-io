using Sprechen.Service.Core.Services;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Macht die interne PCM-Sendeverzögerung für Benchmarks öffentlich zugänglich.
/// </summary>
public sealed class PcmAudioStreamPacerAdapter
{
    private readonly PcmAudioStreamPacer _pacer = new();

    public ValueTask PaceAsync(int bytesSent, int sampleRateHz, CancellationToken ct)
    {
        return _pacer.PaceAsync(bytesSent, sampleRateHz, ct);
    }
}
