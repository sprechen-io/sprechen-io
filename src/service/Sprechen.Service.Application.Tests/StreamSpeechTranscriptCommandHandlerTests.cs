using Moq;
using Xunit;
using Sprechen.Service.Application.Cqrs.SpeechTranscription;

namespace Sprechen.Service.Application.Tests;

public sealed class StreamSpeechTranscriptCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_StreamsConversationEventsFromProvider()
    {
        var conversational = new Mock<IConversationalSpeechToTextService>(MockBehavior.Strict);
        conversational
            .Setup(service => service.StreamConversationAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de"))
            .Returns(StreamEvents(
                new SpeechConversationTranscriptFinalizedEvent("hallo", 80, null),
                new SpeechConversationCompletedEvent()));
        var handler = new StreamSpeechTranscriptCommandHandler(conversational.Object);

        var collected = new List<SpeechConversationEvent>();
        var stream = await handler.HandleAsync(new StreamSpeechTranscriptCommand(EmptyChunks(), AudioMimeType.Pcm, "de"), CancellationToken.None);
        await foreach (var conversationEvent in stream)
        {
            collected.Add(conversationEvent);
        }

        Assert.Equal(2, collected.Count);
        Assert.IsType<SpeechConversationTranscriptFinalizedEvent>(collected[0]);
        Assert.IsType<SpeechConversationCompletedEvent>(collected[1]);
        conversational.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenLanguageCodeIsBlankThrows()
    {
        var conversational = new Mock<IConversationalSpeechToTextService>(MockBehavior.Strict);
        var handler = new StreamSpeechTranscriptCommandHandler(conversational.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new StreamSpeechTranscriptCommand(EmptyChunks(), AudioMimeType.Pcm, "  "), CancellationToken.None));
    }

    [Fact]
    public void Constructor_WhenConversationalServiceIsNullThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new StreamSpeechTranscriptCommandHandler(null!));
    }

    private static async IAsyncEnumerable<SpeechConversationEvent> StreamEvents(params SpeechConversationEvent[] events)
    {
        foreach (var conversationEvent in events)
        {
            await Task.Yield();
            yield return conversationEvent;
        }
    }

    private static async IAsyncEnumerable<byte[]> EmptyChunks()
    {
        await Task.Yield();
        yield break;
    }
}
