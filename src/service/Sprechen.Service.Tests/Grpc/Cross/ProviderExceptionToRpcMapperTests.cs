using Grpc.Core;
using Sprechen.Proto.Transport;
using Sprechen.Service.Grpc;
using Sprechen.Service.Tests.TestDoubles;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Grpc.Cross;

public sealed class ProviderExceptionToRpcMapperTests
{
    [Fact]
    public void MapRateLimited_ReturnsCodedResourceExhausted()
    {
        var logger = new RecordingLogger<SpeechGrpcService>();
        var mapper = new ProviderExceptionToRpcMapper(logger);
        var providerException = new SpeechProviderRateLimitedException("Deepgram", "listen");

        var rpcException = mapper.MapRateLimited(providerException, "SpeechToText", "Speech-to-text", "corr-1");

        Assert.Equal(StatusCode.ResourceExhausted, rpcException.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(rpcException, out var errorCode));
        Assert.Equal(SprechenProto.SprechenErrorCode.ProviderRateLimited, errorCode);
        Assert.Contains(logger.Entries, entry => entry.Message.Contains("corr-1", StringComparison.Ordinal));
    }

    [Fact]
    public void MapProviderFailure_ReturnsCodedInternalFailure()
    {
        var mapper = new ProviderExceptionToRpcMapper(new RecordingLogger<SpeechGrpcService>());

        var rpcException = mapper.MapProviderFailure(
            new InvalidOperationException("provider failed"),
            "TextToSpeechToStream",
            "Text-to-speech",
            clientCorrelationId: null);

        Assert.Equal(StatusCode.Internal, rpcException.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(rpcException, out var errorCode));
        Assert.Equal(SprechenProto.SprechenErrorCode.ProviderFailure, errorCode);
    }
}
