using Google.Protobuf;
using Grpc.Core;
using Sprechen.Proto;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Proto.Transport;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;

using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class TextToSpeechPlayerTests
{
    [Fact]
    public async Task SpeakAsync_WhenGrpcStreamsPcm_StopsEarconsAndInitializesResetsEnqueuesAndDrains()
    {
        var requestCapture = new Sprechen.Proto.TextRequest();
        var responses = new[]
        {
            new Sprechen.Proto.AudioResponse
            {
                MimeType = AudioMimeType.AudioPcm,
                SampleRateHz = PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ,
                Data = ByteString.CopyFrom(new byte[] { 1, 2, 3, 4 })
            },
            new Sprechen.Proto.AudioResponse
            {
                MimeType = AudioMimeType.AudioPcm,
                SampleRateHz = PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ,
                Data = ByteString.CopyFrom(new byte[] { 5, 6, 7, 8 })
            }
        };

        var call = CreateServerStreamingCall(new SequenceAsyncStreamReader<Sprechen.Proto.AudioResponse>(responses));

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.TextToSpeechToStream(
                It.IsAny<Sprechen.Proto.TextRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Sprechen.Proto.TextRequest, Metadata, DateTime?, CancellationToken>((request, _, _, _) => requestCapture = request)
            .Returns(call);

        var pcmPlayer = new Mock<IPcmAudioPlayer>(MockBehavior.Strict);
        pcmPlayer
            .Setup(player => player.InitializeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        pcmPlayer
            .Setup(player => player.ResetAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        pcmPlayer
            .Setup(player => player.SetMakeupAsync(It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        pcmPlayer
            .Setup(player => player.EnqueuePcm16LeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        pcmPlayer
            .Setup(player => player.DrainAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var earconPlayer = CreateEarconPlayer();

        var languagePreferences = new Mock<IUserLanguagePreferences>(MockBehavior.Strict);
        languagePreferences.SetupGet(p => p.SelectedPolitenessLevel).Returns(PolitenessLevel.Formal);

        using var bargeIn = new BargeInCoordinator();
        var sut = CreatePlayer(
            grpcClient.Object,
            pcmPlayer.Object,
            earconPlayer.Object,
            bargeIn,
            languagePreferences.Object);

        await sut.SpeakAsync(" Hallo ", " de ", 1.3d, SpokenAnnouncementKind.Prompt, CancellationToken.None);

        Assert.Equal("Hallo", requestCapture.Text);
        Assert.Equal("de", requestCapture.LanguageCode);
        Assert.Equal(AudioMimeType.AudioPcm, requestCapture.DesiredMimeType);
        Assert.Equal(Sprechen.Proto.TextToSpeechPolitenessLevel.TtsPolitenessFormal, requestCapture.PolitenessLevel);
        Assert.True(requestCapture.HasSpeechRate);
        Assert.Equal(1.3f, requestCapture.SpeechRate);

        pcmPlayer.Verify(player => player.InitializeAsync(PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        pcmPlayer.Verify(player => player.ResetAsync(It.IsAny<CancellationToken>()), Times.Once);
        pcmPlayer.Verify(player => player.EnqueuePcm16LeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        pcmPlayer.Verify(player => player.DrainAsync(It.IsAny<CancellationToken>()), Times.Once);
        earconPlayer.Verify(player => player.StopLoopingAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SpeakAsync_WhenGrpcCallIsCancelled_ResetsPlayerAndThrowsOperationCanceledException()
    {
        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.TextToSpeechToStream(
                It.IsAny<Sprechen.Proto.TextRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Cancelled, "cancelled")));

        var pcmPlayer = new Mock<IPcmAudioPlayer>(MockBehavior.Strict);
        pcmPlayer
            .Setup(player => player.InitializeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        pcmPlayer
            .Setup(player => player.ResetAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        pcmPlayer
            .Setup(player => player.SetMakeupAsync(It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var earconPlayer = CreateEarconPlayer();

        var languagePreferences = new Mock<IUserLanguagePreferences>(MockBehavior.Strict);
        languagePreferences.SetupGet(p => p.SelectedPolitenessLevel).Returns((PolitenessLevel?)null);

        using var bargeIn = new BargeInCoordinator();
        var sut = CreatePlayer(
            grpcClient.Object,
            pcmPlayer.Object,
            earconPlayer.Object,
            bargeIn,
            languagePreferences.Object);

        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.SpeakAsync("Hallo", "de", CancellationToken.None));

        pcmPlayer.Verify(player => player.ResetAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        earconPlayer.Verify(player => player.StopLoopingAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SpeakAsync_WhenGrpcCallFails_ResetsPlayerAndThrowsSpeechErrorException()
    {
        var trailers = SprechenGrpcErrorMetadata.CreateTrailers(SprechenErrorCode.ProviderFailure);

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.TextToSpeechToStream(
                It.IsAny<Sprechen.Proto.TextRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Internal, "down"), trailers));

        var pcmPlayer = new Mock<IPcmAudioPlayer>(MockBehavior.Strict);
        pcmPlayer
            .Setup(player => player.InitializeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        pcmPlayer
            .Setup(player => player.ResetAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        pcmPlayer
            .Setup(player => player.SetMakeupAsync(It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var earconPlayer = CreateEarconPlayer();

        var languagePreferences = new Mock<IUserLanguagePreferences>(MockBehavior.Strict);
        languagePreferences.SetupGet(p => p.SelectedPolitenessLevel).Returns((PolitenessLevel?)null);

        using var bargeIn = new BargeInCoordinator();
        var sut = CreatePlayer(
            grpcClient.Object,
            pcmPlayer.Object,
            earconPlayer.Object,
            bargeIn,
            languagePreferences.Object);

        var exception = await Assert.ThrowsAsync<SpeechErrorException>(() => sut.SpeakAsync("Hallo", "de", CancellationToken.None));

        Assert.Equal(SpeechErrorCode.ProviderFailure, exception.ErrorCode);
        Assert.Equal("down", exception.Message);

        pcmPlayer.Verify(player => player.ResetAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        earconPlayer.Verify(player => player.StopLoopingAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SpeakAsync_WhenGrpcReturnsNoAudio_ResetsPlayerAndThrowsInvalidOperationException()
    {
        var call = CreateServerStreamingCall(new SequenceAsyncStreamReader<Sprechen.Proto.AudioResponse>(Array.Empty<Sprechen.Proto.AudioResponse>()));

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.TextToSpeechToStream(
                It.IsAny<Sprechen.Proto.TextRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(call);

        var pcmPlayer = new Mock<IPcmAudioPlayer>(MockBehavior.Strict);
        pcmPlayer
            .Setup(player => player.InitializeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        pcmPlayer
            .Setup(player => player.ResetAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        pcmPlayer
            .Setup(player => player.SetMakeupAsync(It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var earconPlayer = CreateEarconPlayer();

        var languagePreferences = new Mock<IUserLanguagePreferences>(MockBehavior.Strict);
        languagePreferences.SetupGet(p => p.SelectedPolitenessLevel).Returns((PolitenessLevel?)null);

        using var bargeIn = new BargeInCoordinator();
        var sut = CreatePlayer(
            grpcClient.Object,
            pcmPlayer.Object,
            earconPlayer.Object,
            bargeIn,
            languagePreferences.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SpeakAsync("Hallo", "de", CancellationToken.None));

        Assert.Contains("no audio", exception.Message, StringComparison.OrdinalIgnoreCase);
        // Ein Reset bereitet die Wiedergabe vor, der zweite räumt nach dem Fehler auf.
        pcmPlayer.Verify(player => player.ResetAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        earconPlayer.Verify(player => player.StopLoopingAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SpeakAsync_WhenPcmEnqueueFailsAfterScheduledAudio_ResetsPlayerAndRethrows()
    {
        var responses = new[]
        {
            new Sprechen.Proto.AudioResponse
            {
                MimeType = AudioMimeType.AudioPcm,
                SampleRateHz = PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ,
                Data = ByteString.CopyFrom(new byte[] { 1, 2, 3, 4 })
            },
            new Sprechen.Proto.AudioResponse
            {
                MimeType = AudioMimeType.AudioPcm,
                SampleRateHz = PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ,
                Data = ByteString.CopyFrom(new byte[] { 5, 6, 7, 8 })
            }
        };
        var call = CreateServerStreamingCall(new SequenceAsyncStreamReader<Sprechen.Proto.AudioResponse>(responses));

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.TextToSpeechToStream(
                It.IsAny<Sprechen.Proto.TextRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(call);

        var pcmPlayer = new Mock<IPcmAudioPlayer>(MockBehavior.Strict);
        pcmPlayer
            .Setup(player => player.InitializeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        pcmPlayer
            .Setup(player => player.ResetAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        pcmPlayer
            .Setup(player => player.SetMakeupAsync(It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        pcmPlayer
            .SetupSequence(player => player.EnqueuePcm16LeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask)
            .Throws(new InvalidOperationException("scheduled enqueue failed"));

        var earconPlayer = CreateEarconPlayer();
        var languagePreferences = new Mock<IUserLanguagePreferences>(MockBehavior.Strict);
        languagePreferences.SetupGet(p => p.SelectedPolitenessLevel).Returns((PolitenessLevel?)null);

        using var bargeIn = new BargeInCoordinator();
        var sut = CreatePlayer(
            grpcClient.Object,
            pcmPlayer.Object,
            earconPlayer.Object,
            bargeIn,
            languagePreferences.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SpeakAsync("Hallo", "de", CancellationToken.None));

        Assert.Equal("scheduled enqueue failed", exception.Message);
        pcmPlayer.Verify(
            player => player.EnqueuePcm16LeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        // Nach dem vorbereitenden Reset muss der Fehler auch den bereits eingereihten ersten Block stoppen.
        pcmPlayer.Verify(player => player.ResetAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        pcmPlayer.Verify(player => player.DrainAsync(It.IsAny<CancellationToken>()), Times.Never);
        earconPlayer.Verify(player => player.StopLoopingAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SpeakAsync_WhenDrainExceedsTimeout_ResetsPlayerAndThrowsSpeechErrorException()
    {
        var responses = new[]
        {
            new Sprechen.Proto.AudioResponse
            {
                MimeType = AudioMimeType.AudioPcm,
                SampleRateHz = PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ,
                Data = ByteString.CopyFrom(new byte[] { 1, 2, 3, 4 })
            }
        };

        var call = CreateServerStreamingCall(new SequenceAsyncStreamReader<Sprechen.Proto.AudioResponse>(responses));

        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.TextToSpeechToStream(
                It.IsAny<Sprechen.Proto.TextRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(call);

        var pcmPlayer = new Mock<IPcmAudioPlayer>(MockBehavior.Strict);
        pcmPlayer
            .Setup(player => player.InitializeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        pcmPlayer
            .Setup(player => player.ResetAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        pcmPlayer
            .Setup(player => player.SetMakeupAsync(It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        pcmPlayer
            .Setup(player => player.EnqueuePcm16LeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        // Drain bleibt bis zum Tokenabbruch offen, damit der Timeout die Fehlerbereinigung auslösen muss.
        pcmPlayer
            .Setup(player => player.DrainAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => new ValueTask(Task.Delay(Timeout.Infinite, ct)));

        var earconPlayer = CreateEarconPlayer();

        var languagePreferences = new Mock<IUserLanguagePreferences>(MockBehavior.Strict);
        languagePreferences.SetupGet(p => p.SelectedPolitenessLevel).Returns((PolitenessLevel?)null);

        using var bargeIn = new BargeInCoordinator();
        var sut = CreatePlayer(
            grpcClient.Object,
            pcmPlayer.Object,
            earconPlayer.Object,
            bargeIn,
            languagePreferences.Object,
            drainTimeout: TimeSpan.FromMilliseconds(50));

        var exception = await Assert.ThrowsAsync<SpeechErrorException>(() => sut.SpeakAsync("Hallo", "de", CancellationToken.None));

        Assert.Equal(SpeechErrorCode.ProviderFailure, exception.ErrorCode);
        pcmPlayer.Verify(player => player.DrainAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Neben der Vorbereitung wird beim Timeout erneut zurückgesetzt.
        pcmPlayer.Verify(player => player.ResetAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        earconPlayer.Verify(player => player.StopLoopingAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IEarconPlayer> CreateEarconPlayer()
    {
        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        // Vor TTS soll nur der laufende Verarbeitungston enden; deshalb ist ausschließlich StopLoopingAsync eingerichtet.
        earconPlayer.Setup(player => player.StopLoopingAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        return earconPlayer;
    }

    private static TextToSpeechPlayer CreatePlayer(
        SpeechGrpcClient grpcClient,
        IPcmAudioPlayer pcmPlayer,
        IEarconPlayer earconPlayer,
        IBargeInCoordinator bargeInCoordinator,
        IUserLanguagePreferences languagePreferences,
        TimeSpan? drainTimeout = null)
    {
        var pcmPlayback = new PcmAudioPlaybackLifecycle(
            pcmPlayer,
            NullLogger<PcmAudioPlaybackLifecycle>.Instance);
        var playbackRunner = drainTimeout is { } timeout
            ? new TextToSpeechPlaybackRunner(
                grpcClient,
                new TextToSpeechRequestFactory(languagePreferences),
                new TextToSpeechPcmStreamPlayer(pcmPlayback, new TtsOutputLoudnessMakeup()),
                new TextToSpeechEarconFeedback(earconPlayer, NullLogger<TextToSpeechEarconFeedback>.Instance, EarconId.ValidationAsrFail),
                bargeInCoordinator,
                new TextToSpeechPlaybackState(),
                new TextToSpeechPlaybackCancellationState(),
                TimeSpan.FromSeconds(15))
            {
                DrainTimeout = timeout
            }
            : new TextToSpeechPlaybackRunner(
                grpcClient,
                new TextToSpeechRequestFactory(languagePreferences),
                new TextToSpeechPcmStreamPlayer(pcmPlayback, new TtsOutputLoudnessMakeup()),
                new TextToSpeechEarconFeedback(earconPlayer, NullLogger<TextToSpeechEarconFeedback>.Instance, EarconId.ValidationAsrFail),
                bargeInCoordinator,
                new TextToSpeechPlaybackState(),
                new TextToSpeechPlaybackCancellationState(),
                TimeSpan.FromSeconds(15));

        return new TextToSpeechPlayer(playbackRunner, TestMicGateInfrastructure.NoOpObserver());
    }

    private static AsyncServerStreamingCall<Sprechen.Proto.AudioResponse> CreateServerStreamingCall(IAsyncStreamReader<Sprechen.Proto.AudioResponse> responseStream)
    {
        return new AsyncServerStreamingCall<Sprechen.Proto.AudioResponse>(
            responseStream,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }
}
