using Google.Protobuf;
using Grpc.Core;
using Sprechen.Service.Core.Services;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Teilt Audio in gRPC-Antwortsegmente und verzögert bei Bedarf den PCM-Versand entsprechend der Audiodauer.
/// </summary>
internal sealed class GrpcAudioResponseStreamWriter
{
    private const int DEFAULT_AUDIO_CHUNK_SIZE_BYTES = 64 * 1024;
    private const int PCM_AUDIO_CHUNK_SIZE_BYTES = 4 * 1024;
    private const int DEFAULT_PCM_SAMPLE_RATE_HZ = 24_000;

    private readonly PcmAudioStreamPacer? _pacer;

    public GrpcAudioResponseStreamWriter(bool pacePcm)
    {
        _pacer = pacePcm ? new PcmAudioStreamPacer() : null;
    }

    public async Task WriteAsync(
        IServerStreamWriter<SprechenProto.AudioResponse> responseStream,
        AudioSynthesisChunk chunk,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(responseStream);

        if (chunk.Data.Length == 0)
        {
            return;
        }

        var mimeType = GrpcAudioMimeTypeMapper.ToProto(chunk.Mime);
        var sampleRateHz = ResolveSampleRateHz(mimeType, chunk.SampleRateHz);
        var chunkSize = ResolveChunkSizeBytes(mimeType);

        for (var offset = 0; offset < chunk.Data.Length; offset += chunkSize)
        {
            ct.ThrowIfCancellationRequested();

            var count = Math.Min(chunkSize, chunk.Data.Length - offset);
            var data = offset == 0 && count == chunk.Data.Length
                ? UnsafeByteOperations.UnsafeWrap(chunk.Data)
                : ByteString.CopyFrom(chunk.Data, offset, count);

            await responseStream.WriteAsync(new SprechenProto.AudioResponse
            {
                Data = data,
                MimeType = mimeType,
                SampleRateHz = sampleRateHz
            }).ConfigureAwait(false);

            if (mimeType == SprechenProto.AudioMimeType.AudioPcm && _pacer is not null)
            {
                await _pacer.PaceAsync(count, sampleRateHz, ct).ConfigureAwait(false);
            }
        }
    }

    private static int ResolveChunkSizeBytes(SprechenProto.AudioMimeType mimeType) =>
        mimeType == SprechenProto.AudioMimeType.AudioPcm ? PCM_AUDIO_CHUNK_SIZE_BYTES : DEFAULT_AUDIO_CHUNK_SIZE_BYTES;

    private static int ResolveSampleRateHz(SprechenProto.AudioMimeType mimeType, int sampleRateHz)
    {
        if (mimeType != SprechenProto.AudioMimeType.AudioPcm)
        {
            return sampleRateHz;
        }

        return sampleRateHz > 0 ? sampleRateHz : DEFAULT_PCM_SAMPLE_RATE_HZ;
    }
}
