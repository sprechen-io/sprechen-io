using Moq;
using Sprechen.Service.Core.Services;
using Xunit;

namespace Sprechen.Service.Core.Tests;

public sealed class RecognitionProfileOrchestratorThreadingTests
{
    [Theory]
    [InlineData(RecognitionProfile.Spelling)]
    [InlineData(RecognitionProfile.Digits)]
    [InlineData(RecognitionProfile.Conversational)]
    public async Task TranscribeStreamingAsync_ForwardsRecognitionProfileToProviderPort(RecognitionProfile profile)
    {
        var captured = (RecognitionProfile?)null;
        var sttProvider = new Mock<ISpeechToTextProvider>(MockBehavior.Strict);
        sttProvider
            .Setup(provider => provider.TranscribeStreamingAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>()))
            .Callback((IAsyncEnumerable<byte[]> _, AudioMimeType _, CancellationToken _, string? _, RecognitionProfile p, SpeechKeyterms? _, string? _, bool _) => captured = p)
            .ReturnsAsync(new SpeechTranscript("hallo", 80));
        var service = CreateService(sttProvider: sttProvider.Object);

        await service.TranscribeStreamingAsync(EmptyChunks(), AudioMimeType.Pcm, CancellationToken.None, "de", profile);

        Assert.Equal(profile, captured);
        sttProvider.VerifyAll();
    }

    [Fact]
    public async Task TranscribeStreamingAsync_WhenProfileOmitted_DefaultsToConversational()
    {
        var captured = (RecognitionProfile?)null;
        var sttProvider = new Mock<ISpeechToTextProvider>(MockBehavior.Strict);
        sttProvider
            .Setup(provider => provider.TranscribeStreamingAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de",
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>()))
            .Callback((IAsyncEnumerable<byte[]> _, AudioMimeType _, CancellationToken _, string? _, RecognitionProfile p, SpeechKeyterms? _, string? _, bool _) => captured = p)
            .ReturnsAsync(new SpeechTranscript("hallo", 80));
        var service = CreateService(sttProvider: sttProvider.Object);

        await service.TranscribeStreamingAsync(EmptyChunks(), AudioMimeType.Pcm, CancellationToken.None, "de");

        Assert.Equal(RecognitionProfile.Conversational, captured);
    }

    [Theory]
    [InlineData(RecognitionProfile.Spelling)]
    [InlineData(RecognitionProfile.Digits)]
    [InlineData(RecognitionProfile.Conversational)]
    public async Task StreamConversationAsync_ForwardsRecognitionProfileToProviderPort(RecognitionProfile profile)
    {
        var captured = (RecognitionProfile?)null;
        var conversationalProvider = new Mock<IConversationalSpeechToTextProvider>(MockBehavior.Strict);
        conversationalProvider
            .Setup(provider => provider.StreamConversationAsync(
                It.IsAny<IAsyncEnumerable<byte[]>>(),
                AudioMimeType.Pcm,
                It.IsAny<CancellationToken>(),
                "de",
                It.IsAny<RecognitionProfile>()))
            .Callback((IAsyncEnumerable<byte[]> _, AudioMimeType _, CancellationToken _, string? _, RecognitionProfile p) => captured = p)
            .Returns(EmptyConversationStream());
        var service = CreateService(conversationalProvider: conversationalProvider.Object);

        await foreach (var _ in service.StreamConversationAsync(EmptyChunks(), AudioMimeType.Pcm, CancellationToken.None, "de", profile))
        {
        }

        Assert.Equal(profile, captured);
        conversationalProvider.VerifyAll();
    }

    private static SpeechService CreateService(
        ISpeechToTextProvider? sttProvider = null,
        IConversationalSpeechToTextProvider? conversationalProvider = null)
    {
        var supportedLanguages = new TestSupportedLanguagesService();
        return new SpeechService(
            new SpeechTranscriptionOrchestrator(
                sttProvider ?? Mock.Of<ISpeechToTextProvider>(MockBehavior.Strict),
                supportedLanguages),
            new SpeechSynthesisOrchestrator(
                Mock.Of<ITextToSpeechProvider>(MockBehavior.Strict),
                supportedLanguages),
            new SpeechConversationStreamOrchestrator(
                conversationalProvider ?? new ThrowingConversationalSpeechToTextProvider(),
                supportedLanguages));
    }

    private static async IAsyncEnumerable<byte[]> EmptyChunks()
    {
        await Task.Yield();
        yield break;
    }

    private static async IAsyncEnumerable<SpeechConversationEvent> EmptyConversationStream()
    {
        await Task.Yield();
        yield break;
    }
}
