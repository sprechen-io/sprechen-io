using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Proto;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class TextToSpeechPlaybackRunnerFaultPhaseTests
{
    private const string ObservedTransportFaultDetail =
        "Error reading next message. HttpRequestException: TypeError: network error JSException: TypeError: network error";

    [Fact]
    public async Task SpeakAsync_WhenFirstStreamReadFailsWithMeasuredInternalNetworkError_RetriesOnceThenRethrows()
    {
        var enqueueCalls = 0;
        var streamCalls = 0;
        var sut = CreatePlayer(
            CreateStreamingClient(
                () =>
                {
                    streamCalls++;
                    return CreateCall(
                        new SequenceAsyncStreamReader<AudioResponse>(
                            Array.Empty<AudioResponse>(),
                            new RpcException(new Status(StatusCode.Internal, ObservedTransportFaultDetail))));
                }),
            CreatePcmPlayer(enqueueCalls: () => enqueueCalls++));

        var exception = await Assert.ThrowsAsync<SpeechErrorException>(
            () => sut.SpeakAsync("Hallo", "de", CancellationToken.None));

        Assert.Equal(SpeechErrorCode.Unspecified, exception.ErrorCode);
        Assert.Equal(ObservedTransportFaultDetail, exception.Message);
        Assert.Equal(2, streamCalls);
        Assert.Equal(0, enqueueCalls);
    }

    [Fact]
    public async Task SpeakAsync_WhenFirstReadFailsMeasuredNetworkError_RetriesOnceAndCompletesSecondAttempt()
    {
        var enqueueCalls = 0;
        var streamCalls = 0;
        var resetCalls = 0;
        var makeupCalls = 0;
        var disposedCalls = 0;
        var sut = CreatePlayer(
            CreateStreamingClient(
                () =>
                {
                    streamCalls++;
                    if (streamCalls == 2)
                    {
                        Assert.Equal(1, disposedCalls);
                    }

                    if (streamCalls == 1)
                    {
                        return CreateCall(
                            new SequenceAsyncStreamReader<AudioResponse>(
                                Array.Empty<AudioResponse>(),
                                new RpcException(new Status(StatusCode.Internal, ObservedTransportFaultDetail))),
                            onDispose: () => disposedCalls++);
                    }

                    return CreateCall(
                        new SequenceAsyncStreamReader<AudioResponse>([CreateChunk()]),
                        onDispose: () => disposedCalls++);
                }),
            CreatePcmPlayer(
                enqueueCalls: () => enqueueCalls++,
                resetCalls: () =>
                {
                    if (streamCalls >= 1)
                    {
                        Assert.True(disposedCalls >= 1);
                    }

                    resetCalls++;
                },
                makeupCalls: () => makeupCalls++));

        await sut.SpeakAsync("Hallo", "de", CancellationToken.None);

        Assert.Equal(2, streamCalls);
        Assert.Equal(1, enqueueCalls);
        Assert.True(resetCalls >= 2);
        Assert.True(makeupCalls >= 2);
        Assert.Equal(2, disposedCalls);
    }

    [Fact]
    public async Task SpeakAsync_WhenLaterStreamReadFailsAfterOneEnqueue_DoesNotOpenASecondCall()
    {
        var enqueueCalls = 0;
        var streamCalls = 0;
        var sut = CreatePlayer(
            CreateStreamingClient(
                () =>
                {
                    streamCalls++;
                    return CreateCall(
                        new SequenceAsyncStreamReader<AudioResponse>(
                            [CreateChunk()],
                            new RpcException(new Status(StatusCode.Internal, ObservedTransportFaultDetail))));
                }),
            CreatePcmPlayer(enqueueCalls: () => enqueueCalls++));

        await Assert.ThrowsAsync<SpeechErrorException>(
            () => sut.SpeakAsync("Hallo", "de", CancellationToken.None));

        Assert.Equal(1, streamCalls);
        Assert.Equal(1, enqueueCalls);
    }

    [Fact]
    public async Task SpeakAsync_WhenCallCreationThrowsMeasuredInternal_DoesNotRetry()
    {
        var streamCalls = 0;
        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.TextToSpeechToStream(
                It.IsAny<TextRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                streamCalls++;
                throw new RpcException(new Status(StatusCode.Internal, ObservedTransportFaultDetail));
            });
        var sut = CreatePlayer(grpcClient, CreatePcmPlayer());

        await Assert.ThrowsAsync<SpeechErrorException>(
            () => sut.SpeakAsync("Hallo", "de", CancellationToken.None));

        Assert.Equal(1, streamCalls);
    }

    [Fact]
    public async Task SpeakAsync_WhenEmptyStreamSucceeds_DoesNotRetry()
    {
        var streamCalls = 0;
        var sut = CreatePlayer(
            CreateStreamingClient(
                () =>
                {
                    streamCalls++;
                    return CreateCall(new SequenceAsyncStreamReader<AudioResponse>(Array.Empty<AudioResponse>()));
                }),
            CreatePcmPlayer());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SpeakAsync("Hallo", "de", CancellationToken.None));

        Assert.Equal(1, streamCalls);
    }

    [Fact]
    public async Task SpeakAsync_WhenFailedToFetchWithoutReadingNextMessage_DoesNotRetry()
    {
        var streamCalls = 0;
        var sut = CreatePlayer(
            CreateStreamingClient(
                () =>
                {
                    streamCalls++;
                    return CreateCall(
                        new SequenceAsyncStreamReader<AudioResponse>(
                            Array.Empty<AudioResponse>(),
                            new RpcException(
                                new Status(StatusCode.Internal, "HttpRequestException: TypeError: Failed to fetch"))));
                }),
            CreatePcmPlayer());

        await Assert.ThrowsAsync<SpeechErrorException>(
            () => sut.SpeakAsync("Hallo", "de", CancellationToken.None));

        Assert.Equal(1, streamCalls);
    }

    [Fact]
    public async Task SpeakAsync_WhenEnqueueThrowsAfterMarkStarted_DoesNotRetry()
    {
        var streamCalls = 0;
        var pcmPlayer = CreatePcmPlayer();
        pcmPlayer
            .Setup(player => player.EnqueuePcm16LeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("enqueue failed"));
        var sut = CreatePlayer(
            CreateStreamingClient(
                () =>
                {
                    streamCalls++;
                    return CreateCall(new SequenceAsyncStreamReader<AudioResponse>([CreateChunk()]));
                }),
            pcmPlayer);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SpeakAsync("Hallo", "de", CancellationToken.None));

        Assert.Equal(1, streamCalls);
    }

    [Fact]
    public async Task SpeakAsync_WhenDrainThrowsAfterEnqueue_DoesNotRetry()
    {
        var streamCalls = 0;
        var pcmPlayer = CreatePcmPlayer();
        pcmPlayer
            .Setup(player => player.DrainAsync(It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("drain failed"));
        var sut = CreatePlayer(
            CreateStreamingClient(
                () =>
                {
                    streamCalls++;
                    return CreateCall(new SequenceAsyncStreamReader<AudioResponse>([CreateChunk()]));
                }),
            pcmPlayer);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SpeakAsync("Hallo", "de", CancellationToken.None));

        Assert.Equal(1, streamCalls);
    }

    [Fact]
    public async Task SpeakAsync_WhenCallerCancelsDuringMeasuredFirstRead_DoesNotRetry()
    {
        var streamCalls = 0;
        var cts = new CancellationTokenSource();
        var sut = CreatePlayer(
            CreateStreamingClient(
                () =>
                {
                    streamCalls++;
                    return CreateCall(
                        new ActionThenFaultStreamReader(
                            cts.Cancel,
                            new RpcException(new Status(StatusCode.Internal, ObservedTransportFaultDetail))));
                }),
            CreatePcmPlayer());

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.SpeakAsync("Hallo", "de", cts.Token));

        Assert.Equal(1, streamCalls);
    }

    [Fact]
    public async Task SpeakAsync_WhenBargeInCancelsSnapshotDuringMeasuredFirstRead_DoesNotRetry()
    {
        var streamCalls = 0;
        var bargeIn = new BargeInCoordinator();
        var sut = CreatePlayer(
            CreateStreamingClient(
                () =>
                {
                    streamCalls++;
                    return CreateCall(
                        new ActionThenFaultStreamReader(
                            bargeIn.BargeIn,
                            new RpcException(new Status(StatusCode.Internal, ObservedTransportFaultDetail))));
                }),
            CreatePcmPlayer(),
            bargeInCoordinator: bargeIn);

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.SpeakAsync("Hallo", "de", CancellationToken.None));

        Assert.Equal(1, streamCalls);
    }

    [Fact]
    public async Task SpeakAsync_WhenPlaybackSupersessionCancelsDuringMeasuredFirstRead_DoesNotRetry()
    {
        var streamCalls = 0;
        var playbackCancellation = new TextToSpeechPlaybackCancellationState();
        var sut = CreatePlayer(
            CreateStreamingClient(
                () =>
                {
                    streamCalls++;
                    return CreateCall(
                        new ActionThenFaultStreamReader(
                            () => playbackCancellation.Replace(),
                            new RpcException(new Status(StatusCode.Internal, ObservedTransportFaultDetail))));
                }),
            CreatePcmPlayer(),
            playbackCancellation: playbackCancellation);

        await Assert.ThrowsAnyAsync<Exception>(
            () => sut.SpeakAsync("Hallo", "de", CancellationToken.None));

        Assert.Equal(1, streamCalls);
    }

    [Fact]
    public async Task SpeakAsync_RetryKeepsOneLogicalPlaybackIdAcrossPhysicalAttempts()
    {
        var logger = new RecordingLogger<TextToSpeechPlaybackRunner>();
        var streamCalls = 0;
        var sut = CreatePlayer(
            CreateStreamingClient(
                () =>
                {
                    streamCalls++;
                    if (streamCalls == 1)
                    {
                        return CreateCall(
                            new SequenceAsyncStreamReader<AudioResponse>(
                                Array.Empty<AudioResponse>(),
                                new RpcException(new Status(StatusCode.Internal, ObservedTransportFaultDetail))));
                    }

                    return CreateCall(new SequenceAsyncStreamReader<AudioResponse>([CreateChunk()]));
                }),
            CreatePcmPlayer(),
            logger: logger);

        await sut.SpeakAsync("Hallo", "de", CancellationToken.None);

        var started = logger.Entries.Where(entry => entry.EventId.Id == 4001).ToArray();
        var attempts = logger.Entries.Where(entry => entry.EventId.Id == 4002).ToArray();
        var completed = logger.Entries.Where(entry => entry.EventId.Id == 4004).ToArray();
        Assert.Single(started);
        Assert.Equal(2, attempts.Length);
        Assert.Single(completed);
        var logicalId = ExtractGuid(started[0].Message).ToString();
        Assert.Contains(logicalId, attempts[0].Message, StringComparison.Ordinal);
        Assert.Contains(logicalId, attempts[1].Message, StringComparison.Ordinal);
        Assert.Contains(logicalId, completed[0].Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Recovered", string.Join('\n', logger.Entries.Select(entry => entry.Message)), StringComparison.Ordinal);
        Assert.DoesNotContain("Hallo", string.Join('\n', logger.Entries.Select(entry => entry.Message)), StringComparison.Ordinal);
    }

    private static Guid ExtractGuid(string message)
    {
        foreach (var token in message.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Guid.TryParse(token, out var id))
            {
                return id;
            }
        }

        throw new InvalidOperationException($"No GUID in '{message}'.");
    }

    private static AudioResponse CreateChunk()
    {
        return new AudioResponse
        {
            MimeType = AudioMimeType.AudioPcm,
            SampleRateHz = PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ,
            Data = ByteString.CopyFrom(new byte[] { 1, 2, 3, 4 })
        };
    }

    private static Mock<IPcmAudioPlayer> CreatePcmPlayer(
        Action? enqueueCalls = null,
        Action? resetCalls = null,
        Action? makeupCalls = null)
    {
        var pcmPlayer = new Mock<IPcmAudioPlayer>(MockBehavior.Strict);
        pcmPlayer
            .Setup(player => player.InitializeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        pcmPlayer
            .Setup(player => player.ResetAsync(It.IsAny<CancellationToken>()))
            .Callback(() => resetCalls?.Invoke())
            .Returns(ValueTask.CompletedTask);
        pcmPlayer
            .Setup(player => player.SetMakeupAsync(It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Callback(() => makeupCalls?.Invoke())
            .Returns(ValueTask.CompletedTask);
        pcmPlayer
            .Setup(player => player.EnqueuePcm16LeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Callback(() => enqueueCalls?.Invoke())
            .Returns(ValueTask.CompletedTask);
        pcmPlayer
            .Setup(player => player.DrainAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        return pcmPlayer;
    }

    private static Mock<SpeechGrpcClient> CreateStreamingClient(
        Func<AsyncServerStreamingCall<AudioResponse>> callFactory)
    {
        var grpcClient = new Mock<SpeechGrpcClient>(MockBehavior.Strict, new NoOpCallInvoker());
        grpcClient
            .Setup(client => client.TextToSpeechToStream(
                It.IsAny<TextRequest>(),
                It.IsAny<Metadata>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(() => callFactory());
        return grpcClient;
    }

    private static TextToSpeechPlayer CreatePlayer(
        Mock<SpeechGrpcClient> grpcClient,
        Mock<IPcmAudioPlayer> pcmPlayer,
        IBargeInCoordinator? bargeInCoordinator = null,
        TextToSpeechPlaybackCancellationState? playbackCancellation = null,
        ILogger<TextToSpeechPlaybackRunner>? logger = null)
    {
        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer
            .Setup(player => player.StopLoopingAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var languagePreferences = new Mock<IUserLanguagePreferences>(MockBehavior.Strict);
        languagePreferences.SetupGet(p => p.SelectedPolitenessLevel).Returns((PolitenessLevel?)null);

        var pcmPlayback = new PcmAudioPlaybackLifecycle(
            pcmPlayer.Object,
            NullLogger<PcmAudioPlaybackLifecycle>.Instance);
        var playbackRunner = new TextToSpeechPlaybackRunner(
            grpcClient.Object,
            new TextToSpeechRequestFactory(languagePreferences.Object),
            new TextToSpeechPcmStreamPlayer(pcmPlayback, new TtsOutputLoudnessMakeup()),
            new TextToSpeechEarconFeedback(earconPlayer.Object, NullLogger<TextToSpeechEarconFeedback>.Instance, EarconId.ValidationAsrFail),
            bargeInCoordinator ?? new BargeInCoordinator(),
            new TextToSpeechPlaybackState(),
            playbackCancellation ?? new TextToSpeechPlaybackCancellationState(),
            TimeSpan.FromSeconds(15),
            logger: logger);

        return new TextToSpeechPlayer(playbackRunner, TestMicGateInfrastructure.NoOpObserver());
    }

    private static AsyncServerStreamingCall<AudioResponse> CreateCall(
        IAsyncStreamReader<AudioResponse> responseStream,
        Action? onDispose = null)
    {
        return new AsyncServerStreamingCall<AudioResponse>(
            responseStream,
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => onDispose?.Invoke());
    }
}
