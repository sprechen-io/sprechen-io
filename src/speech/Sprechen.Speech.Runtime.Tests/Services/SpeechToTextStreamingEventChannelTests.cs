namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechToTextStreamingEventChannelTests
{
    [Fact]
    public async Task WriteAsync_WhenTheReaderConsumes_PreservesBothEventsInOrder()
    {
        var events = new SpeechToTextStreamingEventChannel();
        var emitted = new List<SpeechConversationEvent>();

        await events.WriteAsync(
            new SpeechConversationTranscriptUpdateEvent("first", Confidence: null, TurnIndex: null),
            CancellationToken.None);
        var reading = ReadIntoAsync(events.Events, emitted);
        await events.WriteAsync(
            new SpeechConversationTranscriptFinalizedEvent("second", Confidence: null, TurnIndex: null),
            CancellationToken.None);
        events.Complete();

        await reading;

        Assert.Equal(2, emitted.Count);
        var update = Assert.IsType<SpeechConversationTranscriptUpdateEvent>(emitted[0]);
        Assert.Equal("first", update.Transcript);
        var finalized = Assert.IsType<SpeechConversationTranscriptFinalizedEvent>(emitted[1]);
        Assert.Equal("second", finalized.Transcript);
    }

    private static async Task ReadIntoAsync(
        IAsyncEnumerable<SpeechConversationEvent> events,
        List<SpeechConversationEvent> emitted)
    {
        await foreach (var conversationEvent in events)
        {
            emitted.Add(conversationEvent);
        }
    }
}
