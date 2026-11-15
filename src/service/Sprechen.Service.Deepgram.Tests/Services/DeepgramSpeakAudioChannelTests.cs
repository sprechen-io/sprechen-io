using Deepgram.Models.Speak.v2.WebSocket;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Service.Deepgram.Services;
using Xunit;
using DeepgramSpeakWebSocketClient = global::Deepgram.Clients.Interfaces.v2.ISpeakWebSocketClient;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class DeepgramSpeakAudioChannelTests
{
    [Fact]
    public async Task SubscribeAsync_WhenAudioArrives_PublishesBytes()
    {
        EventHandler<AudioResponse>? onAudio = null;
        EventHandler<FlushedResponse>? onFlushed = null;
        var wsClient = CreateClient(
            audioHandler => onAudio = audioHandler,
            flushedHandler => onFlushed = flushedHandler);
        var channel = new DeepgramSpeakAudioChannel(
            "https://api.deepgram.com/",
            "aura-2-test",
            NullLogger<DeepgramSpeakAudioChannel>.Instance);

        await channel.SubscribeAsync(wsClient.Object);
        var readTask = channel.Reader.ReadAsync().AsTask();

        onAudio?.Invoke(null, new AudioResponse { Stream = new MemoryStream([1, 2, 3]) });
        onFlushed?.Invoke(null, new FlushedResponse());

        var bytes = await readTask;
        Assert.Equal([1, 2, 3], bytes);
        await channel.Reader.Completion;
    }

    [Fact]
    public async Task SubscribeAsync_WhenErrorArrives_CompletesWithProviderFailure()
    {
        EventHandler<ErrorResponse>? onError = null;
        var wsClient = CreateClient(captureError: errorHandler => onError = errorHandler);
        var channel = new DeepgramSpeakAudioChannel(
            "https://api.deepgram.com/",
            "aura-2-test",
            NullLogger<DeepgramSpeakAudioChannel>.Instance);

        await channel.SubscribeAsync(wsClient.Object);

        onError?.Invoke(null, new ErrorResponse { Message = "rate limit" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => channel.Reader.Completion);
        Assert.Contains("rate limit", ex.Message, StringComparison.Ordinal);
        Assert.Contains("aura-2-test", ex.Message, StringComparison.Ordinal);
    }

    private static Mock<DeepgramSpeakWebSocketClient> CreateClient(
        Action<EventHandler<AudioResponse>>? captureAudio = null,
        Action<EventHandler<FlushedResponse>>? captureFlushed = null,
        Action<EventHandler<ErrorResponse>>? captureError = null)
    {
        var wsClient = new Mock<DeepgramSpeakWebSocketClient>(MockBehavior.Strict);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<AudioResponse>>()))
            .Callback<EventHandler<AudioResponse>>(handler => captureAudio?.Invoke(handler))
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<WarningResponse>>()))
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<ErrorResponse>>()))
            .Callback<EventHandler<ErrorResponse>>(handler => captureError?.Invoke(handler))
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<FlushedResponse>>()))
            .Callback<EventHandler<FlushedResponse>>(handler => captureFlushed?.Invoke(handler))
            .ReturnsAsync(true);
        return wsClient;
    }
}
