using Moq;
using Sprechen.Service.Core.Services;
using Xunit;

namespace Sprechen.Service.Core.Tests;

public sealed class SpeechServiceExtensionsTests
{
    [Fact]
    public async Task TranscribeStreamingAsync_PassesNullLanguageCode()
    {
        var service = new Mock<ISpeechService>(MockBehavior.Strict);
        service.Setup(s => s.TranscribeStreamingAsync(It.IsAny<IAsyncEnumerable<byte[]>>(), AudioMimeType.Pcm, CancellationToken.None, null))
            .ReturnsAsync(new SpeechTranscript("ok", 0));

        var chunks = GetChunks(new byte[] { 1, 2 });
        var result = await service.Object.TranscribeStreamingAsync(chunks, AudioMimeType.Pcm, CancellationToken.None);

        Assert.Equal("ok", result.Text);
        service.VerifyAll();
    }

    [Fact]
    public async Task TranscribeStreamingFeed_PassesNullLanguageCode()
    {
        var service = new Mock<ISpeechService>(MockBehavior.Strict);
        service.Setup(s => s.TranscribeStreamingFeed(It.IsAny<IAsyncEnumerable<byte[]>>(), AudioMimeType.Pcm, CancellationToken.None, null))
            .Returns(GetFeed("a", "b"));

        var chunks = GetChunks(new byte[] { 1, 2 });
        var results = new List<SpeechTranscript>();
        await foreach (var token in service.Object.TranscribeStreamingFeed(chunks, AudioMimeType.Pcm, CancellationToken.None))
        {
            results.Add(token);
        }

        Assert.Collection(
            results,
            first => Assert.Equal("a", first.Text),
            second => Assert.Equal("b", second.Text));
        service.VerifyAll();
    }

    [Fact]
    public async Task SynthesizeAsync_OverloadsApplyDefaultSpeedAndLanguageCode()
    {
        var chunk = new AudioSynthesisChunk(new byte[] { 1 }, AudioMimeType.Pcm);
        var service = new Mock<ISpeechService>(MockBehavior.Strict);

        service.Setup(s => s.SynthesizeAsync(
                "hi",
                AudioMimeType.Pcm,
                1.0f,
                null,
                TextToSpeechPolitenessLevel.Unspecified,
                CancellationToken.None))
            .Returns(() => GetChunks(chunk));

        service.Setup(s => s.SynthesizeAsync(
                "hi",
                AudioMimeType.Pcm,
                1.0f,
                "de",
                TextToSpeechPolitenessLevel.Unspecified,
                CancellationToken.None))
            .Returns(() => GetChunks(chunk));

        service.Setup(s => s.SynthesizeAsync(
                "hi",
                AudioMimeType.Pcm,
                1.0f,
                "de",
                TextToSpeechPolitenessLevel.Formal,
                CancellationToken.None))
            .Returns(() => GetChunks(chunk));

        _ = await ToListAsync(service.Object.SynthesizeAsync("hi", AudioMimeType.Pcm));
        _ = await ToListAsync(service.Object.SynthesizeAsync("hi", AudioMimeType.Pcm, CancellationToken.None));
        _ = await ToListAsync(service.Object.SynthesizeAsync("hi", AudioMimeType.Pcm, "de", CancellationToken.None));
        _ = await ToListAsync(service.Object.SynthesizeAsync(
            "hi",
            AudioMimeType.Pcm,
            "de",
            TextToSpeechPolitenessLevel.Formal,
            CancellationToken.None));

        service.Verify(s => s.SynthesizeAsync(
                "hi",
                AudioMimeType.Pcm,
                1.0f,
                null,
                TextToSpeechPolitenessLevel.Unspecified,
                CancellationToken.None),
            Times.Exactly(2));
        service.Verify(s => s.SynthesizeAsync(
                "hi",
                AudioMimeType.Pcm,
                1.0f,
                "de",
                TextToSpeechPolitenessLevel.Unspecified,
                CancellationToken.None),
            Times.Once);
        service.Verify(s => s.SynthesizeAsync(
                "hi",
                AudioMimeType.Pcm,
                1.0f,
                "de",
                TextToSpeechPolitenessLevel.Formal,
                CancellationToken.None),
            Times.Once);
        service.VerifyNoOtherCalls();
    }

    private static async Task<IReadOnlyList<AudioSynthesisChunk>> ToListAsync(IAsyncEnumerable<AudioSynthesisChunk> chunks)
    {
        var list = new List<AudioSynthesisChunk>();
        await foreach (var chunk in chunks)
        {
            list.Add(chunk);
        }

        return list;
    }

    private static async IAsyncEnumerable<byte[]> GetChunks(params byte[][] chunks)
    {
        foreach (var chunk in chunks)
        {
            await Task.Yield();
            yield return chunk;
        }
    }

    private static async IAsyncEnumerable<SpeechTranscript> GetFeed(params string[] tokens)
    {
        foreach (var token in tokens)
        {
            await Task.Yield();
            yield return new SpeechTranscript(token, null);
        }
    }

    private static async IAsyncEnumerable<AudioSynthesisChunk> GetChunks(params AudioSynthesisChunk[] chunks)
    {
        foreach (var chunk in chunks)
        {
            await Task.Yield();
            yield return chunk;
        }
    }
}
