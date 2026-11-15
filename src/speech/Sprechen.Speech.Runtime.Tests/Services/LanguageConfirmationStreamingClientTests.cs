using Bunit;
using Grpc.Core;
using Sprechen.Proto;
using Moq;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;
using GrpcSpeechConversationEvent = Sprechen.Proto.SpeechConversationEventMessage;
using AppSupportedLanguage = Sprechen.Speech.LanguageContracts.SupportedLanguage;
using AppSupportedLanguages = Sprechen.Speech.LanguageContracts.SupportedLanguages;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class LanguageConfirmationStreamingClientTests : TestContext
{
    [Fact]
    public async Task StartAsync_WhenInvoked_CreatesLocalChunkSourceAndReturnsSession()
    {
        await using var chunkSource = new ControlledSpeechAudioChunkSource(SpeechAudioMimeType.Pcm);
        var chunkSourceFactory = new FixedSpeechCaptureLocalSessionChunkSourceFactory(chunkSource);

        var bargeIn = new Mock<IUserSpeechBargeInHandler>();
        bargeIn
            .Setup(h => h.OnSpeechCaptureStartingAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var speechRequestWriter = new CollectingClientStreamWriter<AudioChunk>();
        var speechResponseReader = new SequenceAsyncStreamReader<GrpcSpeechConversationEvent>(new[]
        {
            CreateTranscriptFinalized("ok")
        });
        var speechCall = new AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>(
            speechRequestWriter,
            speechResponseReader,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

        var supportedLanguagesClient = new Mock<ISupportedLanguagesClient>(MockBehavior.Strict);
        supportedLanguagesClient
            .Setup(client => client.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSupportedLanguages("de", new[]
            {
                new AppSupportedLanguage("de", "Deutsch")
            }));

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.SpeechToConversationStream(
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(speechCall);

        var client = new LanguageConfirmationStreamingClient(
            chunkSourceFactory,
            bargeIn.Object,
            supportedLanguagesClient.Object,
            grpcClient.Object);

        var session = await client.StartAsync(languageCode: "de", cancellationToken: CancellationToken.None);

        Assert.IsType<LanguageConfirmationStreamingSession>(session);

        bargeIn.Verify(h => h.OnSpeechCaptureStartingAsync(It.IsAny<CancellationToken>()), Times.Once);
        supportedLanguagesClient.Verify(client => client.GetAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(1, chunkSourceFactory.CreateCallCount);
        grpcClient.VerifyAll();

        chunkSource.PushChunk(new byte[] { 1, 2 });
        chunkSource.Complete();
        await session.StopAsync(CancellationToken.None);
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

        var supportedLanguagesClient = new Mock<ISupportedLanguagesClient>(MockBehavior.Strict);
        supportedLanguagesClient
            .Setup(client => client.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSupportedLanguages("de", new[]
            {
                new AppSupportedLanguage("de", "Deutsch")
            }));

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.SpeechToConversationStream(
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("boom"));

        var client = new LanguageConfirmationStreamingClient(
            chunkSourceFactory,
            bargeIn.Object,
            supportedLanguagesClient.Object,
            grpcClient.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.StartAsync(languageCode: "de", cancellationToken: CancellationToken.None));
        Assert.Contains("boom", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, chunkSource.DisposeCallCount);
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

