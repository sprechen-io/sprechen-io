using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Sprechen.Speech.App.GrpcInfrastructure.Extensions;
using Sprechen.Proto;
using Sprechen.Mda.Contracts.V1.Catalog;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class TextToSpeechFirstPcmTimeoutTests
{
    private readonly Mock<IPcmAudioPlayer> _pcm = new(MockBehavior.Strict);
    private readonly Mock<IEarconPlayer> _earcon = new(MockBehavior.Strict);
    private readonly Mock<ITextToSpeechPlaybackState> _state = new(MockBehavior.Strict);
    private readonly Mock<SpeechGrpcClient> _grpc = new(MockBehavior.Strict, new NoOpCallInvoker());
    private readonly BargeInCoordinator _barge = new();
    private readonly TextToSpeechPlaybackCancellationState _cancellation = new();
    private readonly Mock<TimeProvider> _clock = new(MockBehavior.Strict);
    private readonly List<Action> _expire = [];
    private readonly List<TimeSpan> _dueTimes = [];
    private int _timersDisposed;
    private int _calls;
    private int _disposed;

    public TextToSpeechFirstPcmTimeoutTests()
    {
        _pcm.Setup(p => p.InitializeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _pcm.Setup(p => p.ResetAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        _pcm.Setup(p => p.SetMakeupAsync(It.IsAny<double>(), It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        _pcm.Setup(p => p.EnqueuePcm16LeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        _pcm.Setup(p => p.DrainAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        _earcon.Setup(p => p.StopLoopingAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        _earcon.Setup(p => p.PlayAsync(EarconId.ValidationAsrFail, It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        _state.Setup(p => p.NotifyPlaybackStarted());
        _state.Setup(p => p.NotifyPlaybackCompleted());
        _clock.Setup(c => c.CreateTimer(It.IsAny<TimerCallback>(), It.IsAny<object?>(), It.IsAny<TimeSpan>(), Timeout.InfiniteTimeSpan))
            .Returns((TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan _) =>
            {
                var timer = new Mock<ITimer>(MockBehavior.Strict);
                timer.Setup(t => t.DisposeAsync()).Callback(() => _timersDisposed++).Returns(ValueTask.CompletedTask);
                _expire.Add(() => callback(state));
                _dueTimes.Add(dueTime);
                return timer.Object;
            });
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public async Task SpeakAsync_WhenNoAlignedPcmArrives_EndsWithProviderFailureAndLocalCue(int prefixBytes)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reader = new Mock<IAsyncStreamReader<AudioResponse>>(MockBehavior.Strict);
        reader.SetupGet(r => r.Current).Returns(new AudioResponse
        {
            MimeType = AudioMimeType.AudioPcm,
            SampleRateHz = PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ,
            Data = ByteString.CopyFrom(new byte[Math.Max(0, prefixBytes)])
        });
        var prefixPending = prefixBytes >= 0;
        reader.Setup(r => r.MoveNext(It.IsAny<CancellationToken>())).Returns(async (CancellationToken token) =>
        {
            if (prefixPending)
            {
                prefixPending = false;
                return true;
            }

            entered.TrySetResult();
            await Task.Delay(Timeout.Infinite, token);
            return false;
        });
        SetReader(reader.Object);
        var runner = CreateRunner();
        using var watchdog = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var task = runner.SpeakAsync("Synthetic prompt", "de", 1, SpokenAnnouncementKind.Prompt, watchdog.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        _expire[0]();

        var error = await Assert.ThrowsAsync<SpeechErrorException>(() => task);

        Assert.Equal(SpeechErrorCode.ProviderFailure, error.ErrorCode);
        Assert.Equal(1, _calls);
        Assert.Equal(1, _disposed);
        _pcm.Verify(p => p.EnqueuePcm16LeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        _pcm.Verify(p => p.ResetAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
        _earcon.Verify(p => p.PlayAsync(EarconId.ValidationAsrFail, It.IsAny<CancellationToken>()), Times.Once);
        _state.Verify(p => p.NotifyPlaybackStarted(), Times.Once);
        _state.Verify(p => p.NotifyPlaybackCompleted(), Times.Once);
        Assert.Equal(1, _timersDisposed);
    }

    [Fact]
    public async Task SpeakAsync_WhenExpiryAndRetryEligibleFailureCoincide_DoesNotRetry()
    {
        var reader = new Mock<IAsyncStreamReader<AudioResponse>>(MockBehavior.Strict);
        reader.Setup(r => r.MoveNext(It.IsAny<CancellationToken>())).Returns(() =>
        {
            _expire[0]();
            throw NetworkFailure();
        });
        SetReader(reader.Object);

        var error = await Assert.ThrowsAsync<SpeechErrorException>(() => SpeakAsync(CreateRunner()));

        Assert.Equal(SpeechErrorCode.ProviderFailure, error.ErrorCode);
        Assert.Equal(1, _calls);
        Assert.Equal(1, _disposed);
        Assert.Single(_dueTimes);
    }

    [Fact]
    public async Task SpeakAsync_WhenSecondAttemptStalls_UsesOriginalBudget()
    {
        var first = new Mock<IAsyncStreamReader<AudioResponse>>(MockBehavior.Strict);
        first.Setup(r => r.MoveNext(It.IsAny<CancellationToken>())).Throws(NetworkFailure());
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = StallingReader(entered);
        SetReaders(call => call == 1 ? first.Object : second);
        var task = SpeakAsync(CreateRunner());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Single(_expire);
        _expire[0]();
        var error = await Assert.ThrowsAsync<SpeechErrorException>(() => task);

        Assert.Equal(SpeechErrorCode.ProviderFailure, error.ErrorCode);
        Assert.Equal(2, _calls);
        Assert.Equal(2, _disposed);
        _earcon.Verify(p => p.PlayAsync(EarconId.ValidationAsrFail, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SpeakAsync_WhenFirstEnqueueWins_LaterExpiryDoesNotCancelLongStream()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        var streamToken = CancellationToken.None;
        var reader = new Mock<IAsyncStreamReader<AudioResponse>>(MockBehavior.Strict);
        reader.SetupGet(r => r.Current).Returns(Chunk());
        reader.Setup(r => r.MoveNext(It.IsAny<CancellationToken>())).Returns(async (CancellationToken token) =>
        {
            if (++reads == 1) return true;
            streamToken = token;
            entered.TrySetResult();
            await finish.Task.WaitAsync(token);
            return false;
        });
        SetReader(reader.Object);
        var task = SpeakAsync(CreateRunner());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        _expire[0]();
        Assert.False(streamToken.IsCancellationRequested);
        Assert.False(task.IsCompleted);
        finish.SetResult();
        await task.WaitAsync(TimeSpan.FromSeconds(2));

        _pcm.Verify(p => p.DrainAsync(It.IsAny<CancellationToken>()), Times.Once);
        _earcon.Verify(p => p.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(1, _timersDisposed);
    }

    [Fact]
    public async Task SpeakAsync_WhenExpiryWins_LateEnqueueCannotRevivePlayback()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _pcm.Setup(p => p.EnqueuePcm16LeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns((byte[] _, CancellationToken _) => new ValueTask(EnqueueAsync()));
        async Task EnqueueAsync()
        {
            entered.TrySetResult();
            await finish.Task;
        }
        SetReader(OneChunkReader());
        var task = SpeakAsync(CreateRunner());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        _expire[0]();
        finish.SetResult();

        var error = await Assert.ThrowsAsync<SpeechErrorException>(() => task);

        Assert.Equal(SpeechErrorCode.ProviderFailure, error.ErrorCode);
        Assert.Equal(1, _calls);
        Assert.Equal(1, _disposed);
        _pcm.Verify(p => p.DrainAsync(It.IsAny<CancellationToken>()), Times.Never);
        _pcm.Verify(p => p.ResetAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public async Task SpeakAsync_WhenIntentionalCancellationWins_DoesNotPlayTimeoutCue(int cause, bool expireFirst)
    {
        using var caller = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reader = new Mock<IAsyncStreamReader<AudioResponse>>(MockBehavior.Strict);
        reader.Setup(r => r.MoveNext(It.IsAny<CancellationToken>())).Returns(async () =>
        {
            entered.TrySetResult();
            await finish.Task;
            throw new RpcException(new Status(StatusCode.Cancelled, "synthetic cancelled read"));
        });
        SetReader(reader.Object);
        var task = SpeakAsync(CreateRunner(), caller.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        if (expireFirst) _expire[0]();
        CancellationTokenSource? replacement = null;
        switch (cause)
        {
            case 0: caller.Cancel(); break;
            case 1: _barge.BargeIn(); break;
            case 2: replacement = _cancellation.Replace(); break;
        }
        finish.SetResult();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);

        _earcon.Verify(p => p.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(1, _calls);
        Assert.Equal(1, _disposed);
        if (replacement is not null) _cancellation.DisposeIfCurrent(replacement);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SpeakAsync_WhenTimeoutCleanupFinishes_FollowingCallCanPlay(bool cueFails)
    {
        if (cueFails)
            _earcon.Setup(p => p.PlayAsync(EarconId.ValidationAsrFail, It.IsAny<CancellationToken>()))
                .Throws(new InvalidOperationException("synthetic audio device failure"));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        SetReaders(call => call == 1 ? StallingReader(entered) : OneChunkReader());
        var runner = CreateRunner();
        var first = SpeakAsync(runner);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        _expire[0]();
        var error = await Assert.ThrowsAsync<SpeechErrorException>(() => first);
        Assert.Equal(SpeechErrorCode.ProviderFailure, error.ErrorCode);

        await SpeakAsync(runner).WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(2, _calls);
        Assert.Equal(2, _disposed);
        Assert.Equal(2, _timersDisposed);
        _state.Verify(p => p.NotifyPlaybackStarted(), Times.Exactly(2));
        _state.Verify(p => p.NotifyPlaybackCompleted(), Times.Exactly(2));
        _pcm.Verify(p => p.EnqueuePcm16LeAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SpeakAsync_WhenTimeoutCueStartsAnotherPlayback_DoesNotHoldPlaybackGate()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        SetReaders(call => call == 1 ? StallingReader(entered) : OneChunkReader());
        var runner = CreateRunner();
        var nextCompleted = false;
        _earcon.Setup(p => p.PlayAsync(EarconId.ValidationAsrFail, It.IsAny<CancellationToken>()))
            .Returns(() => new ValueTask(PlayNextAsync()));
        async Task PlayNextAsync()
        {
            Assert.Equal(1, _timersDisposed);
            await SpeakAsync(runner).WaitAsync(TimeSpan.FromSeconds(2));
            nextCompleted = true;
        }
        var first = SpeakAsync(runner);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        _expire[0]();

        // Der verschachtelte neue Aufruf löst den bereits abgelaufenen Aufruf ab.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);

        Assert.True(nextCompleted);
        Assert.Equal(2, _calls);
        Assert.Equal(2, _disposed);
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(23000)]
    [InlineData(60000)]
    public async Task SpeakAsync_WhenModelBudgetChanges_RegisteredRunnerUsesThatBudget(int milliseconds)
    {
        var model = VoiceInterfaceModelLoader.Load();
        model.FirstPcmEnqueueTimeoutMs = milliseconds;
        Assert.True(Sprechen.Mda.Contracts.V1.Preflight.VoiceInterfacePreflight.Validate(model).Ok);
        var catalog = new VoiceInterfaceModelCatalogCore(model);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSpeechGrpcInfrastructure(catalog.FirstPcmEnqueueTimeout, catalog.FirstPcmTimeoutEarconId);
        services.AddSingleton(_grpc.Object);
        services.AddSingleton(_pcm.Object);
        services.AddSingleton(_earcon.Object);
        services.AddSingleton(_state.Object);
        services.AddSingleton<IBargeInCoordinator>(_barge);
        services.AddSingleton(Mock.Of<IUserLanguagePreferences>());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<TextToSpeechPlaybackRunner>();
        // Den per DI erzeugten Runner behalten und nur seine Uhr für den kontrollierten Ablauf ersetzen.
        typeof(TextToSpeechPlaybackRunner).GetProperty(nameof(TextToSpeechPlaybackRunner.TimeProvider),
            BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(runner, _clock.Object);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        SetReader(StallingReader(entered));
        var task = SpeakAsync(runner);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(TimeSpan.FromMilliseconds(milliseconds), Assert.Single(_dueTimes));
        _expire[0]();
        var error = await Assert.ThrowsAsync<SpeechErrorException>(() => task);

        Assert.Equal(SpeechErrorCode.ProviderFailure, error.ErrorCode);
        _earcon.Verify(p => p.PlayAsync(catalog.FirstPcmTimeoutEarconId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SpeakAsync_WhenPreparationExpires_DoesNotOpenTransport(bool duringInitialize)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (duringInitialize)
            _pcm.Setup(p => p.InitializeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((int _, CancellationToken token) => StallAsync(token));
        else
            _pcm.Setup(p => p.ResetAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken token) => token.CanBeCanceled ? new ValueTask(StallAsync(token)) : ValueTask.CompletedTask);
        async Task StallAsync(CancellationToken token)
        {
            entered.TrySetResult();
            await Task.Delay(Timeout.Infinite, token);
        }
        var task = SpeakAsync(CreateRunner());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        _expire[0]();

        var error = await Assert.ThrowsAsync<SpeechErrorException>(() => task);

        Assert.Equal(SpeechErrorCode.ProviderFailure, error.ErrorCode);
        Assert.Equal(0, _calls);
        _earcon.Verify(p => p.PlayAsync(EarconId.ValidationAsrFail, It.IsAny<CancellationToken>()), Times.Once);
        _state.Verify(p => p.NotifyPlaybackCompleted(), Times.Once);
    }

    [Fact]
    public async Task SpeakAsync_WhenGrpcReportsDeadlineAsCancelled_ReturnsProviderFailure()
    {
        var reader = new Mock<IAsyncStreamReader<AudioResponse>>(MockBehavior.Strict);
        reader.Setup(r => r.MoveNext(It.IsAny<CancellationToken>())).Returns(() =>
        {
            _expire[0]();
            throw new RpcException(new Status(StatusCode.Cancelled, "synthetic transport cancellation"));
        });
        SetReader(reader.Object);

        var error = await Assert.ThrowsAsync<SpeechErrorException>(() => SpeakAsync(CreateRunner()));

        Assert.Equal(SpeechErrorCode.ProviderFailure, error.ErrorCode);
        Assert.Equal(1, _calls);
        Assert.Equal(1, _disposed);
    }

    [Theory]
    [InlineData("selection")]
    [InlineData("form-speak")]
    [InlineData("form-observe")]
    public async Task PromptConsumers_WhenFirstPcmExpires_PropagateTechnicalFailure(string consumer)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        SetReader(StallingReader(entered));
        var player = new TextToSpeechPlayer(CreateRunner(), Mock.Of<IMicGateState>());
        var form = new FormVoiceCommandPromptPlayback(player, NullLogger.Instance);
        var task = consumer switch
        {
            "selection" => new SelectionDialogPromptSpeaker(player, NullLogger.Instance).SpeakAsync(
                SelectionDialogPrompt.FromText("Synthetic prompt"), "de",
                new SelectionDialogInteractionSettings(1, 0, true, 85, 65), CancellationToken.None),
            "form-speak" => form.SpeakSafeAsync("Synthetic prompt", "de", new VoiceDialogSettings(), CancellationToken.None),
            "form-observe" => form.ObserveCompletionAsync(
                form.StartAsync("Synthetic prompt", "de", 1, CancellationToken.None), CancellationToken.None),
            _ => throw new ArgumentOutOfRangeException(nameof(consumer))
        };
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        _expire[0]();

        var error = await Assert.ThrowsAsync<SpeechErrorException>(() => task);

        Assert.Equal(SpeechErrorCode.ProviderFailure, error.ErrorCode);
        Assert.Equal(1, _calls);
    }

    private static Task SpeakAsync(TextToSpeechPlaybackRunner runner, CancellationToken token = default)
        => runner.SpeakAsync("Synthetic prompt", "de", 1, SpokenAnnouncementKind.Prompt, token);

    private static RpcException NetworkFailure() => new(new Status(StatusCode.Internal,
        "Error reading next message. HttpRequestException: TypeError: network error JSException: TypeError: network error"));

    private static AudioResponse Chunk() => new()
    {
        MimeType = AudioMimeType.AudioPcm,
        SampleRateHz = PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ,
        Data = ByteString.CopyFrom(new byte[] { 1, 2, 3, 4 })
    };

    private static IAsyncStreamReader<AudioResponse> OneChunkReader()
    {
        var reader = new Mock<IAsyncStreamReader<AudioResponse>>(MockBehavior.Strict);
        reader.SetupGet(r => r.Current).Returns(Chunk());
        reader.SetupSequence(r => r.MoveNext(It.IsAny<CancellationToken>())).ReturnsAsync(true).ReturnsAsync(false);
        return reader.Object;
    }

    private static IAsyncStreamReader<AudioResponse> StallingReader(TaskCompletionSource entered)
    {
        var reader = new Mock<IAsyncStreamReader<AudioResponse>>(MockBehavior.Strict);
        reader.Setup(r => r.MoveNext(It.IsAny<CancellationToken>())).Returns(async (CancellationToken token) =>
        {
            entered.TrySetResult();
            await Task.Delay(Timeout.Infinite, token);
            return false;
        });
        return reader.Object;
    }

    private void SetReader(IAsyncStreamReader<AudioResponse> reader)
        => SetReaders(_ => reader);

    private void SetReaders(Func<int, IAsyncStreamReader<AudioResponse>> reader)
    {
        _grpc.Setup(c => c.TextToSpeechToStream(It.IsAny<TextRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                _calls++;
                return new AsyncServerStreamingCall<AudioResponse>(reader(_calls), Task.FromResult(new Metadata()),
                    () => Status.DefaultSuccess, () => new Metadata(), () => _disposed++);
            });
    }

    private TextToSpeechPlaybackRunner CreateRunner()
    {
        var preferences = new Mock<IUserLanguagePreferences>();
        return new TextToSpeechPlaybackRunner(_grpc.Object, new(preferences.Object),
            new(new PcmAudioPlaybackLifecycle(_pcm.Object, NullLogger<PcmAudioPlaybackLifecycle>.Instance), new TtsOutputLoudnessMakeup()),
            new(_earcon.Object, NullLogger<TextToSpeechEarconFeedback>.Instance, EarconId.ValidationAsrFail),
            _barge, _state.Object, _cancellation, TimeSpan.FromMilliseconds(50))
        { TimeProvider = _clock.Object };
    }
}
