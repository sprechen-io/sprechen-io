using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Sprechen.Service.Application.Cqrs.SpeechTranscription;

namespace Sprechen.Service.Application.Tests;

public sealed class TranscribeSpeechCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenProviderReturnsTranscriptReturnsTypedResult()
    {
        var speechToText = new Mock<ISpeechToTextService>(MockBehavior.Strict);
        speechToText
            .Setup(service => service.TranscribeStreamingAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de"))
            .ReturnsAsync(new SpeechTranscript("hallo welt", 92));
        var handler = new TranscribeSpeechCommandHandler(speechToText.Object, NullLogger<TranscribeSpeechCommandHandler>.Instance);

        var result = await handler.HandleAsync(new TranscribeSpeechCommand(EmptyChunks(), AudioMimeType.Pcm, "de"), CancellationToken.None);

        Assert.False(result.IsEmpty);
        Assert.Equal("hallo welt", result.Transcript.Text);
        Assert.Equal(92, result.Transcript.ConfidencePercent);
        speechToText.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenProviderReturnsEmptyTranscriptFlagsResultAsEmpty()
    {
        var speechToText = new Mock<ISpeechToTextService>(MockBehavior.Strict);
        speechToText
            .Setup(service => service.TranscribeStreamingAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de"))
            .ReturnsAsync(new SpeechTranscript(string.Empty, null));
        var handler = new TranscribeSpeechCommandHandler(speechToText.Object, NullLogger<TranscribeSpeechCommandHandler>.Instance);

        var result = await handler.HandleAsync(new TranscribeSpeechCommand(EmptyChunks(), AudioMimeType.Pcm, "de"), CancellationToken.None);

        Assert.True(result.IsEmpty);
        Assert.Equal(string.Empty, result.Transcript.Text);
        speechToText.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenLanguageCodeIsBlankThrows()
    {
        var speechToText = new Mock<ISpeechToTextService>(MockBehavior.Strict);
        var handler = new TranscribeSpeechCommandHandler(speechToText.Object, NullLogger<TranscribeSpeechCommandHandler>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new TranscribeSpeechCommand(EmptyChunks(), AudioMimeType.Pcm, "  "), CancellationToken.None));
    }

    [Fact]
    public void Constructor_WhenSpeechToTextIsNullThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new TranscribeSpeechCommandHandler(null!, NullLogger<TranscribeSpeechCommandHandler>.Instance));
    }

    [Fact]
    public async Task HandleAsync_WhenCommandCarriesClientCorrelationId_ForwardsItToTheProvider()
    {
        // Die Korrelationskennung muss vom Kommando unverändert an den Transkriptionsdienst weitergegeben werden.
        var speechToText = new Mock<ISpeechToTextService>(MockBehavior.Strict);
        speechToText
            .Setup(service => service.TranscribeStreamingAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de",
                RecognitionProfile.Conversational,
                null,
                "turn-9f8e7d6c"))
            .ReturnsAsync(new SpeechTranscript("hallo welt", 92));
        var handler = new TranscribeSpeechCommandHandler(speechToText.Object, NullLogger<TranscribeSpeechCommandHandler>.Instance);

        var result = await handler.HandleAsync(
            new TranscribeSpeechCommand(EmptyChunks(), AudioMimeType.Pcm, "de", ClientCorrelationId: "turn-9f8e7d6c"),
            CancellationToken.None);

        Assert.False(result.IsEmpty);
        speechToText.VerifyAll();
    }

    private static async IAsyncEnumerable<byte[]> EmptyChunks()
    {
        await Task.Yield();
        yield break;
    }
}
