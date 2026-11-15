using Sprechen.Service.Grpc;
using Sprechen.Service.Tests.Grpc;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Grpc.Endpoints;

public sealed class SpeechConversationEventStreamWriterTests
{
    [Fact]
    public async Task WriteAsync_WhenEventsContainTranscriptCountsAndWritesMessages()
    {
        var sut = new SpeechConversationEventStreamWriter(GrpcEndpointTestFactory.CreateDiagnosticsEmitter());
        var writer = new RecordingServerStreamWriter<SprechenProto.SpeechConversationEventMessage>();

        var result = await sut.WriteAsync(
            ConversationEvents(
                new SpeechConversationTranscriptUpdateEvent("Hallo", 80, 1),
                new SpeechConversationCompletedEvent()),
            writer,
            CancellationToken.None);

        Assert.Equal(2, result.EventCount);
        Assert.Equal(1, result.TranscriptCount);
        Assert.Collection(
            writer.Items,
            first => Assert.Equal("Hallo", first.TranscriptUpdate.Text),
            second => Assert.NotNull(second.ConversationCompleted));
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
