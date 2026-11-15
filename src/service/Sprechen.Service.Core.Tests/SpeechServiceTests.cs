using Moq;
using Sprechen.Service.Core.Services;
using Xunit;

namespace Sprechen.Service.Core.Tests;

public sealed class SpeechServiceTests
{
    [Fact]
    public async Task SynthesizeAsync_UsesTextToSpeechProviderPort()
    {
        var ttsProvider = new Mock<ITextToSpeechProvider>(MockBehavior.Strict);

        async IAsyncEnumerable<AudioSynthesisChunk> Synth()
        {
            yield return new AudioSynthesisChunk(new byte[] { 1, 2, 3 }, AudioMimeType.Mp3);
            await Task.CompletedTask;
        }

        ttsProvider.Setup(p => p.SynthesizeAsync(
                "hello",
                AudioMimeType.Mp3,
                1.0f,
                "de",
                TextToSpeechPolitenessLevel.Unspecified,
                It.IsAny<CancellationToken>()))
            .Returns(Synth());

        var service = CreateService(ttsProvider: ttsProvider.Object);

        var results = new List<AudioSynthesisChunk>();
        await foreach (var chunk in service.SynthesizeAsync("hello", AudioMimeType.Mp3, "de", CancellationToken.None))
        {
            results.Add(chunk);
        }

        Assert.Single(results);
        Assert.Equal(AudioMimeType.Mp3, results[0].Mime);
        Assert.Equal(new byte[] { 1, 2, 3 }, results[0].Data);
        ttsProvider.VerifyAll();
    }

    [Fact]
    public async Task SynthesizeAsync_WithExplicitPoliteness_ForwardsPolitenessToProvider()
    {
        var ttsProvider = new Mock<ITextToSpeechProvider>(MockBehavior.Strict);

        async IAsyncEnumerable<AudioSynthesisChunk> Synth()
        {
            yield return new AudioSynthesisChunk(new byte[] { 1 }, AudioMimeType.Pcm);
            await Task.CompletedTask;
        }

        ttsProvider.Setup(p => p.SynthesizeAsync(
                "hello",
                AudioMimeType.Pcm,
                1.0f,
                "de",
                TextToSpeechPolitenessLevel.Formal,
                It.IsAny<CancellationToken>()))
            .Returns(Synth());

        var service = CreateService(ttsProvider: ttsProvider.Object);

        await foreach (var _ in service.SynthesizeAsync(
                           "hello",
                           AudioMimeType.Pcm,
                           1.0f,
                           "de",
                           TextToSpeechPolitenessLevel.Formal,
                           CancellationToken.None))
        {
        }

        ttsProvider.VerifyAll();
    }

    private static SpeechService CreateService(
        ISpeechToTextProvider? sttProvider = null,
        IConversationalSpeechToTextProvider? conversationalProvider = null,
        ITextToSpeechProvider? ttsProvider = null,
        ISupportedLanguagesService? supportedLanguages = null)
    {
        supportedLanguages ??= new TestSupportedLanguagesService();

        return new SpeechService(
            new SpeechTranscriptionOrchestrator(
                sttProvider ?? Mock.Of<ISpeechToTextProvider>(MockBehavior.Strict),
                supportedLanguages),
            new SpeechSynthesisOrchestrator(
                ttsProvider ?? Mock.Of<ITextToSpeechProvider>(MockBehavior.Strict),
                supportedLanguages),
            new SpeechConversationStreamOrchestrator(
                conversationalProvider ?? new ThrowingConversationalSpeechToTextProvider(),
                supportedLanguages));
    }
}
