using Grpc.Core;
using Sprechen.Service.Core.Services;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Macht den internen gRPC-Audiowriter für Benchmarks öffentlich zugänglich.
/// </summary>
public sealed class GrpcAudioResponseStreamWriterAdapter
{
    private readonly GrpcAudioResponseStreamWriter _writer;

    public GrpcAudioResponseStreamWriterAdapter(bool pacePcm)
    {
        _writer = new GrpcAudioResponseStreamWriter(pacePcm);
    }

    public Task WriteAsync(
        IServerStreamWriter<SprechenProto.AudioResponse> responseStream,
        AudioSynthesisChunk chunk,
        CancellationToken ct)
    {
        return _writer.WriteAsync(responseStream, chunk, ct);
    }
}
