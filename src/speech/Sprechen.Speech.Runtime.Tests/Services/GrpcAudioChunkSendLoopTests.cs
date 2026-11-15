using Grpc.Core;
using Sprechen.Proto;
using Moq;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class GrpcAudioChunkSendLoopTests
{
    [Fact]
    public async Task WriteAsync_WithCapturedChunks_SendsLanguageCodeOnlyOnFirstNonEmptyChunk()
    {
        List<AudioChunk> messages = [];
        var stream = new Mock<IClientStreamWriter<AudioChunk>>(MockBehavior.Strict);
        stream.SetupProperty(item => item.WriteOptions);
        stream.Setup(item => item.WriteAsync(It.IsAny<AudioChunk>(), It.IsAny<CancellationToken>()))
            .Callback<AudioChunk, CancellationToken>((chunk, _) => messages.Add(chunk))
            .Returns(Task.CompletedTask);
        stream.Setup(item => item.CompleteAsync()).Returns(Task.CompletedTask);

        await new GrpcAudioChunkSendLoop().WriteAsync(
            [[], [1, 2], [3]],
            stream.Object,
            SpeechAudioMimeType.Pcm,
            CancellationToken.None,
            " de ");

        Assert.Equal(2, messages.Count);
        Assert.Equal("de", messages[0].LanguageCode);
        Assert.True(string.IsNullOrWhiteSpace(messages[1].LanguageCode));
        Assert.Equal([1, 2], messages[0].Data.ToByteArray());
        Assert.Equal([3], messages[1].Data.ToByteArray());
        stream.Verify(item => item.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task WriteAsync_WhenWriteFails_CompletesRequestStreamBestEffort()
    {
        var stream = new Mock<IClientStreamWriter<AudioChunk>>(MockBehavior.Strict);
        stream.SetupProperty(item => item.WriteOptions);
        stream.Setup(item => item.WriteAsync(It.IsAny<AudioChunk>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("write failed"));
        stream.Setup(item => item.CompleteAsync()).Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<InvalidOperationException>(() => new GrpcAudioChunkSendLoop().WriteAsync(
            [[1]],
            stream.Object,
            SpeechAudioMimeType.Pcm,
            CancellationToken.None,
            languageCode: null));

        stream.Verify(item => item.CompleteAsync(), Times.Once);
    }
}
