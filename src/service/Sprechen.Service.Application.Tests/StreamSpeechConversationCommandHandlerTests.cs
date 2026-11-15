using Moq;
using Xunit;
using Sprechen.Service.Application.Cqrs.SpeechConversation;

namespace Sprechen.Service.Application.Tests;

public sealed class StreamSpeechConversationCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_StreamsAllConversationEvents()
    {
        var conversational = new Mock<IConversationalSpeechToTextService>(MockBehavior.Strict);
        conversational
            .Setup(service => service.StreamConversationAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                It.Is<string?>(value => value == null)))
            .Returns(StreamEvents(new SpeechConversationCompletedEvent()));
        var handler = new StreamSpeechConversationCommandHandler(conversational.Object);

        var collected = new List<SpeechConversationEvent>();
        var stream = await handler.HandleAsync(new StreamSpeechConversationCommand(EmptyChunks(), AudioMimeType.Pcm, LanguageCode: null), CancellationToken.None);
        await foreach (var conversationEvent in stream)
        {
            collected.Add(conversationEvent);
        }

        Assert.Single(collected);
        Assert.IsType<SpeechConversationCompletedEvent>(collected[0]);
        conversational.VerifyAll();
    }

    [Fact]
    public void Constructor_WhenConversationalServiceIsNullThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new StreamSpeechConversationCommandHandler(null!));
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
