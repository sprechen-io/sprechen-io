using Moq;
using Xunit;

namespace Sprechen.Service.Core.Tests;

public sealed class TextToSpeechServiceExtensionsTests
{
    [Fact]
    public async Task SynthesizeAsync_WithoutCancellationToken_UsesDefaultSpeedAndUnspecifiedPoliteness()
    {
        var chunk = new AudioSynthesisChunk([1], AudioMimeType.Pcm);
        var service = new Mock<ITextToSpeechService>(MockBehavior.Strict);
        service.Setup(s => s.SynthesizeAsync(
                "hallo",
                AudioMimeType.Pcm,
                1.0f,
                null,
                TextToSpeechPolitenessLevel.Unspecified,
                CancellationToken.None))
            .Returns(Chunks(chunk));

        var result = await ToListAsync(service.Object.SynthesizeAsync("hallo", AudioMimeType.Pcm));

        Assert.Single(result);
        service.VerifyAll();
    }

    [Fact]
    public async Task SynthesizeAsync_WithCancellationToken_UsesDefaultSpeedAndUnspecifiedPoliteness()
    {
        using var cts = new CancellationTokenSource();
        var chunk = new AudioSynthesisChunk([2], AudioMimeType.Mp3);
        var service = new Mock<ITextToSpeechService>(MockBehavior.Strict);
        service.Setup(s => s.SynthesizeAsync(
                "hello",
                AudioMimeType.Mp3,
                1.0f,
                null,
                TextToSpeechPolitenessLevel.Unspecified,
                cts.Token))
            .Returns(Chunks(chunk));

        var result = await ToListAsync(service.Object.SynthesizeAsync("hello", AudioMimeType.Mp3, cts.Token));

        Assert.Single(result);
        service.VerifyAll();
    }

    [Fact]
    public async Task SynthesizeAsync_WithLanguage_UsesDefaultSpeedAndUnspecifiedPoliteness()
    {
        using var cts = new CancellationTokenSource();
        var chunk = new AudioSynthesisChunk([3], AudioMimeType.Wav);
        var service = new Mock<ITextToSpeechService>(MockBehavior.Strict);
        service.Setup(s => s.SynthesizeAsync(
                "bonjour",
                AudioMimeType.Wav,
                1.0f,
                "fr",
                TextToSpeechPolitenessLevel.Unspecified,
                cts.Token))
            .Returns(Chunks(chunk));

        var result = await ToListAsync(service.Object.SynthesizeAsync("bonjour", AudioMimeType.Wav, "fr", cts.Token));

        Assert.Single(result);
        service.VerifyAll();
    }

    [Fact]
    public async Task SynthesizeAsync_WithLanguageAndPoliteness_UsesDefaultSpeed()
    {
        using var cts = new CancellationTokenSource();
        var chunk = new AudioSynthesisChunk([4], AudioMimeType.OggOpus);
        var service = new Mock<ITextToSpeechService>(MockBehavior.Strict);
        service.Setup(s => s.SynthesizeAsync(
                "guten tag",
                AudioMimeType.OggOpus,
                1.0f,
                "de",
                TextToSpeechPolitenessLevel.Formal,
                cts.Token))
            .Returns(Chunks(chunk));

        var result = await ToListAsync(service.Object.SynthesizeAsync(
            "guten tag",
            AudioMimeType.OggOpus,
            "de",
            TextToSpeechPolitenessLevel.Formal,
            cts.Token));

        Assert.Single(result);
        service.VerifyAll();
    }

    [Fact]
    public void SynthesizeAsync_WhenServiceIsNull_Throws()
    {
        ITextToSpeechService? service = null;

        Assert.Throws<ArgumentNullException>(() => service!.SynthesizeAsync("text", AudioMimeType.Pcm));
        Assert.Throws<ArgumentNullException>(() => service!.SynthesizeAsync("text", AudioMimeType.Pcm, CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => service!.SynthesizeAsync("text", AudioMimeType.Pcm, "de", CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => service!.SynthesizeAsync(
            "text",
            AudioMimeType.Pcm,
            "de",
            TextToSpeechPolitenessLevel.Formal,
            CancellationToken.None));
    }

    private static async Task<IReadOnlyList<AudioSynthesisChunk>> ToListAsync(
        IAsyncEnumerable<AudioSynthesisChunk> chunks)
    {
        var list = new List<AudioSynthesisChunk>();
        await foreach (var chunk in chunks)
        {
            list.Add(chunk);
        }

        return list;
    }

    private static async IAsyncEnumerable<AudioSynthesisChunk> Chunks(params AudioSynthesisChunk[] chunks)
    {
        foreach (var chunk in chunks)
        {
            await Task.Yield();
            yield return chunk;
        }
    }
}
