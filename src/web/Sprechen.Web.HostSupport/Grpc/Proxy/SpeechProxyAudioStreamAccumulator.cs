using Sprechen.Proto;

namespace Sprechen.Web.HostSupport.Grpc.Proxy;

/// <summary>
/// Zählt nicht leere Audiochunks und ihre Bytes vor dem Weiterleitungsversuch.
/// Die Abtastrate wird nur aus dem ersten gezählten Chunk übernommen, wenn sie positiv ist.
/// </summary>
internal sealed class SpeechProxyAudioStreamAccumulator
{
    public int ChunkCount { get; private set; }

    public long ByteCount { get; private set; }

    public int? FirstSampleRateHz { get; private set; }

    public bool TryAdd(AudioResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.Data is null || response.Data.IsEmpty)
        {
            return false;
        }

        if (ChunkCount == 0 && response.SampleRateHz > 0)
        {
            FirstSampleRateHz = response.SampleRateHz;
        }

        ChunkCount++;
        ByteCount += response.Data.Length;
        return true;
    }

    public SpeechProxyAudioForwardingSummary ToSummary()
    {
        return new SpeechProxyAudioForwardingSummary(ChunkCount, ByteCount, FirstSampleRateHz);
    }
}
