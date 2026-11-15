using Grpc.Core;
using Moq;
using Sprechen.Proto.Transport;
using Sprechen.Service.Grpc;
using Sprechen.Service.Tests.Grpc;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Grpc.Endpoints;

public sealed class SpeechToTextStreamEndpointTests
{
    [Fact]
    public async Task HandleAsync_WhenStreamCompletesWritesTranscripts()
    {
        var language = new Mock<IConversationalSpeechToTextService>(MockBehavior.Strict);
        language.Setup(service => service.StreamConversationAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de"))
            .Returns(StreamConversationResults(
                new SpeechConversationTranscriptFinalizedEvent("fertig", 91, null),
                new SpeechConversationCompletedEvent()));
        var endpoint = CreateEndpoint(language.Object);
        var writer = new RecordingServerStreamWriter<SprechenProto.SpeechTranscript>();

        await endpoint.HandleAsync(
            GrpcEndpointTestFactory.CreatePcmRequestStream(),
            writer,
            GrpcEndpointTestFactory.CreateContext("SpeechToTextStream"));

        var transcript = Assert.Single(writer.Items);
        Assert.Equal("fertig", transcript.Text);
        Assert.True(transcript.HasConfidence);
        Assert.Equal(91, transcript.Confidence);
        language.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenClientCancelsPropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        var language = new Mock<IConversationalSpeechToTextService>(MockBehavior.Strict);
        language.Setup(service => service.StreamConversationAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de"))
            .Returns(CancelledConversationFeed(cts));
        var endpoint = CreateEndpoint(language.Object);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            endpoint.HandleAsync(
                GrpcEndpointTestFactory.CreatePcmRequestStream(),
                new RecordingServerStreamWriter<SprechenProto.SpeechTranscript>(),
                GrpcEndpointTestFactory.CreateContext("SpeechToTextStream", cts.Token)));

        language.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenProviderReportsFailureThrowsStableProviderFailure()
    {
        var language = new Mock<IConversationalSpeechToTextService>(MockBehavior.Strict);
        language.Setup(service => service.StreamConversationAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de"))
            .Returns(StreamConversationResults(new SpeechConversationFailedEvent("provider", "boom")));
        var endpoint = CreateEndpoint(language.Object);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            endpoint.HandleAsync(
                GrpcEndpointTestFactory.CreatePcmRequestStream(),
                new RecordingServerStreamWriter<SprechenProto.SpeechTranscript>(),
                GrpcEndpointTestFactory.CreateContext("SpeechToTextStream")));

        Assert.Equal(StatusCode.Internal, ex.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var errorCode));
        Assert.Equal(SprechenProto.SprechenErrorCode.ProviderFailure, errorCode);
        language.VerifyAll();
    }

    private static SpeechToTextStreamEndpoint CreateEndpoint(IConversationalSpeechToTextService language)
    {
        var supportedLanguages = GrpcEndpointTestFactory.CreateSupportedLanguages();
        return new SpeechToTextStreamEndpoint(
            GrpcEndpointTestFactory.CreateStreamSpeechTranscriptDispatcher(language),
            GrpcEndpointTestFactory.CreateRequestValidator(supportedLanguages.Object),
            GrpcEndpointTestFactory.CreateDiagnosticsEmitter(),
            new SpeechToTextTranscriptStreamWriter(
                GrpcEndpointTestFactory.CreateDiagnosticsEmitter(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<SpeechGrpcService>.Instance),
            GrpcEndpointTestFactory.CreateExceptionMapper(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SpeechGrpcService>.Instance);
    }

    private static async IAsyncEnumerable<SpeechConversationEvent> StreamConversationResults(params SpeechConversationEvent[] results)
    {
        foreach (var result in results)
        {
            await Task.Yield();
            yield return result;
        }
    }

    private static async IAsyncEnumerable<SpeechConversationEvent> CancelledConversationFeed(CancellationTokenSource cancellationTokenSource)
    {
        ArgumentNullException.ThrowIfNull(cancellationTokenSource);

        await Task.Yield();
        cancellationTokenSource.Cancel();
        ThrowCancellation();
        yield break;
    }

    private static void ThrowCancellation()
        => throw new OperationCanceledException();
}
