using Grpc.Core;
using Moq;
using Sprechen.Proto.Transport;
using Sprechen.Service.Grpc;
using Sprechen.Service.Options;
using Sprechen.Service.Tests.Grpc;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Grpc.Endpoints;

public sealed class SpeechToConversationStreamEndpointTests
{
    [Fact]
    public async Task HandleAsync_WhenStreamCompletesWritesConversationEvents()
    {
        var language = new Mock<IConversationalSpeechToTextService>(MockBehavior.Strict);
        language.Setup(service => service.StreamConversationAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de"))
            .Returns(StreamConversationResults(new SpeechConversationCompletedEvent()));
        var endpoint = CreateEndpoint(language.Object);
        var writer = new RecordingServerStreamWriter<SprechenProto.SpeechConversationEventMessage>();

        await endpoint.HandleAsync(
            GrpcEndpointTestFactory.CreatePcmRequestStream(),
            writer,
            GrpcEndpointTestFactory.CreateContext("SpeechToConversationStream"));

        var message = Assert.Single(writer.Items);
        Assert.NotNull(message.ConversationCompleted);
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
                new RecordingServerStreamWriter<SprechenProto.SpeechConversationEventMessage>(),
                GrpcEndpointTestFactory.CreateContext("SpeechToConversationStream", cts.Token)));

        language.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenProviderThrowsRpcExceptionMapsProviderFailure()
    {
        var providerFailure = new RpcException(new Status(StatusCode.NotFound, "provider missing"));
        var language = new Mock<IConversationalSpeechToTextService>(MockBehavior.Strict);
        language.Setup(service => service.StreamConversationAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de"))
            .Returns(ThrowingConversationFeed(providerFailure));
        var endpoint = CreateEndpoint(language.Object);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            endpoint.HandleAsync(
                GrpcEndpointTestFactory.CreatePcmRequestStream(),
                new RecordingServerStreamWriter<SprechenProto.SpeechConversationEventMessage>(),
                GrpcEndpointTestFactory.CreateContext("SpeechToConversationStream")));

        Assert.Equal(StatusCode.Internal, ex.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var errorCode));
        Assert.Equal(SprechenProto.SprechenErrorCode.ProviderFailure, errorCode);
        language.VerifyAll();
    }

    private static SpeechToConversationStreamEndpoint CreateEndpoint(IConversationalSpeechToTextService language)
    {
        var supportedLanguages = GrpcEndpointTestFactory.CreateSupportedLanguages();
        return new SpeechToConversationStreamEndpoint(
            GrpcEndpointTestFactory.CreateStreamSpeechConversationDispatcher(language),
            GrpcEndpointTestFactory.CreateRequestValidator(supportedLanguages.Object),
            new SpeechConversationEventStreamWriter(GrpcEndpointTestFactory.CreateDiagnosticsEmitter()),
            GrpcEndpointTestFactory.CreateExceptionMapper(),
            Microsoft.Extensions.Options.Options.Create(new ConversationStreamingOptions()),
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

    private static async IAsyncEnumerable<SpeechConversationEvent> ThrowingConversationFeed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        await Task.Yield();
        ThrowException(exception);
        yield break;
    }

    private static void ThrowCancellation()
        => throw new OperationCanceledException();

    private static void ThrowException(Exception exception)
        => throw exception;
}
