using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Proto.Transport;
using Sprechen.Service.Grpc;
using Sprechen.Service.Tests.Grpc;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Grpc.Endpoints;

[Collection(SpeechRecognitionTelemetryCollection.Name)]
public sealed class SpeechToTextTranscriptStreamWriterTests
{
    [Fact]
    public async Task WriteAsync_WhenNoTranscriptWritesEmptyTranscript()
    {
        var sut = CreateSut();
        var writer = new RecordingServerStreamWriter<SprechenProto.SpeechTranscript>();

        var count = await sut.WriteAsync(
            ConversationEvents(new SpeechConversationCompletedEvent()),
            writer,
            "de",
            AudioMimeType.Pcm,
            SpeechProvider.Deepgram,
            "correlation",
            "trace",
            CancellationToken.None);

        Assert.Equal(0, count);
        var transcript = Assert.Single(writer.Items);
        Assert.Equal(string.Empty, transcript.Text);
    }

    [Fact]
    public async Task WriteAsync_WhenProviderFailedThrowsStableProviderFailure()
    {
        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            sut.WriteAsync(
                ConversationEvents(new SpeechConversationFailedEvent("provider", "boom")),
                new RecordingServerStreamWriter<SprechenProto.SpeechTranscript>(),
                "de",
                AudioMimeType.Pcm,
                SpeechProvider.Deepgram,
                "correlation",
                "trace",
                CancellationToken.None));

        Assert.Equal(StatusCode.Internal, ex.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var errorCode));
        Assert.Equal(SprechenProto.SprechenErrorCode.ProviderFailure, errorCode);
    }

    private static SpeechToTextTranscriptStreamWriter CreateSut()
    {
        return new SpeechToTextTranscriptStreamWriter(
            GrpcEndpointTestFactory.CreateDiagnosticsEmitter(),
            NullLogger<SpeechGrpcService>.Instance);
    }

    private static async IAsyncEnumerable<SpeechConversationEvent> ConversationEvents(params SpeechConversationEvent[] events)
    {
        foreach (var conversationEvent in events)
        {
            await Task.Yield();
            yield return conversationEvent;
        }
    }
}
