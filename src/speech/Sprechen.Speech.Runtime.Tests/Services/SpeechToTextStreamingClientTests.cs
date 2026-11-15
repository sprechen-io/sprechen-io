using Bunit;
using Grpc.Core;
using Sprechen.Proto;
using Moq;
using Sprechen.Proto.Transport;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;
using GrpcSpeechConversationEvent = Sprechen.Proto.SpeechConversationEventMessage;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechToTextStreamingClientTests : TestContext
{
    [Fact]
    public async Task StartAsync_WhenInvoked_CreatesLocalChunkSourceAndReturnsSession()
    {
        await using var chunkSource = new ControlledSpeechAudioChunkSource(SpeechAudioMimeType.Pcm);
        var chunkSourceFactory = new FixedSpeechCaptureLocalSessionChunkSourceFactory(chunkSource);
        Metadata? capturedHeaders = null;

        var bargeIn = new Mock<IUserSpeechBargeInHandler>();
        bargeIn
            .Setup(h => h.OnSpeechCaptureStartingAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var requestWriter = new CollectingClientStreamWriter<AudioChunk>();
        var responseReader = new SequenceAsyncStreamReader<GrpcSpeechConversationEvent>(new[]
        {
            CreateTranscriptFinalized("ok")
        });
        var call = new AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>(
            requestWriter,
            responseReader,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.SpeechToConversationStream(
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Metadata?, DateTime?, CancellationToken>((headers, _, _) => capturedHeaders = headers)
            .Returns(call);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        var trace = new BrowserDebugConsoleSpeechClientTrace(
            browserDebugConsole,
            new RecordingLogger<BrowserDebugConsoleSpeechClientTrace>());

        var client = new SpeechToTextStreamingClient(chunkSourceFactory, bargeIn.Object, grpcClient.Object, trace);

        var session = await client.StartAsync("de", CancellationToken.None);

        Assert.IsType<SpeechToTextStreamingSession>(session);

        bargeIn.Verify(h => h.OnSpeechCaptureStartingAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(1, chunkSourceFactory.CreateCallCount);
        grpcClient.VerifyAll();
        Assert.True(SprechenGrpcCorrelationMetadata.TryGetCorrelationId(capturedHeaders, out var correlationId));
        Assert.False(string.IsNullOrWhiteSpace(correlationId));

        chunkSource.PushChunk(new byte[] { 1, 2 });
        chunkSource.Complete();
        await session.StopAsync(CancellationToken.None);

        Assert.Contains(
            browserDebugConsole.Entries,
            entry => entry.Contains("speech-conversation: cid=", StringComparison.Ordinal)
                && entry.Contains("speech-to-conversation-stream started", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StartAsync_WhenRemoteOpenFails_DisposesChunkSource()
    {
        await using var chunkSource = new ControlledSpeechAudioChunkSource(SpeechAudioMimeType.Pcm);
        var chunkSourceFactory = new FixedSpeechCaptureLocalSessionChunkSourceFactory(chunkSource);

        var bargeIn = new Mock<IUserSpeechBargeInHandler>();
        bargeIn
            .Setup(h => h.OnSpeechCaptureStartingAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.SpeechToConversationStream(
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("boom"));

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        var trace = new BrowserDebugConsoleSpeechClientTrace(
            browserDebugConsole,
            new RecordingLogger<BrowserDebugConsoleSpeechClientTrace>());

        var client = new SpeechToTextStreamingClient(chunkSourceFactory, bargeIn.Object, grpcClient.Object, trace);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.StartAsync("de", CancellationToken.None));
        Assert.Contains("boom", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, chunkSource.DisposeCallCount);
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => entry.Contains("speech-conversation: cid=", StringComparison.Ordinal)
                && entry.Contains("speech-to-conversation-stream open failed", StringComparison.Ordinal));
    }

    private static SpeechConversationEventMessage CreateTranscriptFinalized(string text)
    {
        return new SpeechConversationEventMessage
        {
            TranscriptFinalized = new SpeechConversationTranscriptFinalized
            {
                Text = text
            }
        };
    }
}


