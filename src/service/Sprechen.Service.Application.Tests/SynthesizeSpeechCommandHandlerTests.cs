using Moq;
using Xunit;
using Sprechen.Service.Application.Cqrs.SpeechSynthesis;

namespace Sprechen.Service.Application.Tests;

public sealed class SynthesizeSpeechCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_YieldsAudioChunksFromProvider()
    {
        var textToSpeech = new Mock<ITextToSpeechService>(MockBehavior.Strict);
        textToSpeech
            .Setup(service => service.SynthesizeAsync(
                "hallo",
                AudioMimeType.Pcm,
                1.0f,
                "de",
                TextToSpeechPolitenessLevel.Formal,
                It.IsAny<CancellationToken>()))
            .Returns(SingleChunk(new byte[] { 0x1, 0x2, 0x3 }));
        var handler = new SynthesizeSpeechCommandHandler(textToSpeech.Object);
        var input = new SynthesizeSpeechInput(
            Text: "hallo",
            LanguageCode: "de",
            DesiredMimeType: AudioMimeType.Pcm,
            SpeechRate: 1.0f,
            PolitenessLevel: TextToSpeechPolitenessLevel.Formal);

        var collected = new List<AudioSynthesisChunk>();
        var stream = await handler.HandleAsync(new SynthesizeSpeechCommand(input), CancellationToken.None);
        await foreach (var chunk in stream)
        {
            collected.Add(chunk);
        }

        Assert.Single(collected);
        Assert.Equal(3, collected[0].Data.Length);
        textToSpeech.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenTextIsBlankThrows()
    {
        var textToSpeech = new Mock<ITextToSpeechService>(MockBehavior.Strict);
        var handler = new SynthesizeSpeechCommandHandler(textToSpeech.Object);
        var input = new SynthesizeSpeechInput("  ", "de", AudioMimeType.Pcm, 1.0f, TextToSpeechPolitenessLevel.Unspecified);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new SynthesizeSpeechCommand(input), CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_WhenInputIsNullThrows()
    {
        var textToSpeech = new Mock<ITextToSpeechService>(MockBehavior.Strict);
        var handler = new SynthesizeSpeechCommandHandler(textToSpeech.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.HandleAsync(new SynthesizeSpeechCommand(null!), CancellationToken.None));
    }

    [Fact]
    public void Constructor_WhenServiceIsNullThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new SynthesizeSpeechCommandHandler(null!));
    }

    private static async IAsyncEnumerable<AudioSynthesisChunk> SingleChunk(byte[] data)
    {
        await Task.Yield();
        yield return new AudioSynthesisChunk(data, AudioMimeType.Pcm, sampleRateHz: 24_000);
    }
}
