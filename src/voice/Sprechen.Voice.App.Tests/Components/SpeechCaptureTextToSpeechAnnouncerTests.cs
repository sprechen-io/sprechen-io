using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Grpc.Core;
using Sprechen.Proto;
using Sprechen.Speech.App.GrpcInfrastructure.Extensions;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Web.SpeechComponents;
using Sprechen.Voice.App.Tests.TestDoubles;

namespace Sprechen.Voice.App.Tests.Components;

public sealed class SpeechCaptureTextToSpeechAnnouncerTests : TestContext
{
    [Fact]
    public async Task OnStopped_WhenTranscriptPresent_SpeaksTranscript()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        RegisterLanguageState("de");
        var logger = RegisterLogger();

        var ttsPlayer = new Mock<ITextToSpeechPlayer>();
        Services.AddSingleton(ttsPlayer.Object);

        var cut = Render<SpeechCaptureTextToSpeechAnnouncer>();

        await messageBus.PublishAsync(new SpeechCaptureStopped("de", "Hallo Welt"));

        cut.WaitForAssertion(() =>
        {
            ttsPlayer.Verify(
                player => player.SpeakAsync(
                    "Hallo Welt",
                    "de",
                    It.IsAny<CancellationToken>()),
                Times.Once);
        });
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task OnStopped_WhenTranscriptEmpty_DoesNotSpeak()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        RegisterLanguageState("de");
        RegisterLogger();

        var ttsPlayer = new Mock<ITextToSpeechPlayer>();
        Services.AddSingleton(ttsPlayer.Object);

        Render<SpeechCaptureTextToSpeechAnnouncer>();

        await messageBus.PublishAsync(new SpeechCaptureStopped("de", " "));

        ttsPlayer.Verify(
            player => player.SpeakAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnStopped_WhenSpeakFails_PublishesFailure()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        RegisterLanguageState("de");
        var logger = RegisterLogger();

        var ttsPlayer = new Mock<ITextToSpeechPlayer>();
        ttsPlayer
            .Setup(player => player.SpeakAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        Services.AddSingleton(ttsPlayer.Object);

        var failureTcs = new TaskCompletionSource<SpeechCaptureFailed>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = messageBus.Subscribe<SpeechCaptureFailed>(message =>
        {
            failureTcs.TrySetResult(message);
            return ValueTask.CompletedTask;
        });

        Render<SpeechCaptureTextToSpeechAnnouncer>();

        await messageBus.PublishAsync(new SpeechCaptureStopped("de", "Hallo Welt"));

        var failure = await failureTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(SpeechErrorCode.Unspecified, failure.ErrorCode);
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingFailed, "de"),
            failure.Message);
        Assert.DoesNotContain("boom", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(logger.Entries, entry => entry.Exception?.Message == "boom");
    }

    [Fact]
    public async Task OnStopped_WhenSpeakCancelled_DoesNotPublishFailure()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        RegisterLanguageState("de");
        var logger = RegisterLogger();

        var ttsPlayer = new Mock<ITextToSpeechPlayer>();
        ttsPlayer
            .Setup(player => player.SpeakAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("cancelled"));
        Services.AddSingleton(ttsPlayer.Object);

        var failureTcs = new TaskCompletionSource<SpeechCaptureFailed>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = messageBus.Subscribe<SpeechCaptureFailed>(message =>
        {
            failureTcs.TrySetResult(message);
            return ValueTask.CompletedTask;
        });

        var cut = Render<SpeechCaptureTextToSpeechAnnouncer>();

        await messageBus.PublishAsync(new SpeechCaptureStopped("de", "Hallo Welt"));

        cut.WaitForAssertion(() =>
        {
            ttsPlayer.Verify(
                player => player.SpeakAsync(
                    "Hallo Welt",
                    "de",
                    It.IsAny<CancellationToken>()),
                Times.Once);
        });

        var completed = await Task.WhenAny(failureTcs.Task, Task.Delay(200));
        Assert.NotSame(failureTcs.Task, completed);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task OnStopped_WhenSpeakThrowsSpeechError_PublishesFailureWithErrorCode()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        RegisterLanguageState("de");
        var logger = RegisterLogger();

        var ttsPlayer = new Mock<ITextToSpeechPlayer>();
        ttsPlayer
            .Setup(player => player.SpeakAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SpeechErrorException(SpeechErrorCode.ProviderFailure, "down"));
        Services.AddSingleton(ttsPlayer.Object);

        var failureTcs = new TaskCompletionSource<SpeechCaptureFailed>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = messageBus.Subscribe<SpeechCaptureFailed>(message =>
        {
            failureTcs.TrySetResult(message);
            return ValueTask.CompletedTask;
        });

        Render<SpeechCaptureTextToSpeechAnnouncer>();

        await messageBus.PublishAsync(new SpeechCaptureStopped("de", "Hallo Welt"));

        var failure = await failureTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(SpeechErrorCode.ProviderFailure, failure.ErrorCode);
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingFailed, "de"),
            failure.Message);
        Assert.DoesNotContain("down", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(logger.Entries, entry => entry.Exception?.Message == "down");
    }

    [Fact]
    public async Task OnStopped_WhenSpeakThrowsOperationCanceled_DoesNotPublishFailure()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        RegisterLanguageState("de");
        var logger = RegisterLogger();

        var ttsPlayer = new Mock<ITextToSpeechPlayer>();
        ttsPlayer
            .Setup(player => player.SpeakAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("cancel"));
        Services.AddSingleton(ttsPlayer.Object);

        var failureTcs = new TaskCompletionSource<SpeechCaptureFailed>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var subscription = messageBus.Subscribe<SpeechCaptureFailed>(message =>
        {
            failureTcs.TrySetResult(message);
            return ValueTask.CompletedTask;
        });

        Render<SpeechCaptureTextToSpeechAnnouncer>();

        await messageBus.PublishAsync(new SpeechCaptureStopped("de", "Hallo Welt"));

        var completed = await Task.WhenAny(failureTcs.Task, Task.Delay(200));
        Assert.NotSame(failureTcs.Task, completed);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task OnFailed_WhenVoiceCommandDialogFails_SpeaksLocalizedFailureAnnouncement()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        RegisterLanguageState("de");
        RegisterLogger();

        var ttsPlayer = new Mock<ITextToSpeechPlayer>();
        Services.AddSingleton(ttsPlayer.Object);

        var cut = Render<SpeechCaptureTextToSpeechAnnouncer>();

        await messageBus.PublishAsync(new SpeechCaptureFailed(SpeechErrorCode.VoiceCommandDialogFailed, "raw failure"));

        var expectedText = ResolveResourceText(
            VoiceInterfaceResourceIds.SpeechCaptureFailureVoiceCommandDialogFailed,
            "de");
        cut.WaitForAssertion(() =>
        {
            ttsPlayer.Verify(
                player => player.SpeakAsync(
                    expectedText,
                    "de",
                    1.0d,
                    SpokenAnnouncementKind.RecoverableFailure,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        });
    }

    [Fact]
    public async Task OnStopped_WhenActualPlaybackTimesOut_FailureAnnouncementIsBoundedAndCuesRemainPerCall()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        RegisterLanguageState("de");
        var settled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loggedErrors = new System.Collections.Concurrent.ConcurrentQueue<Exception>();
        var logger = new Mock<ILogger<SpeechCaptureTextToSpeechAnnouncer>>();
        logger.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                if (invocation.Arguments[3] is Exception error)
                {
                    loggedErrors.Enqueue(error);
                    if (loggedErrors.Count == 2) settled.TrySetResult();
                }
            }));
        Services.AddSingleton(logger.Object);
        Services.AddLogging();
        Services.AddSpeechGrpcInfrastructure(TimeSpan.FromMilliseconds(50), EarconId.ValidationAsrFail);
        Services.AddSingleton(Mock.Of<IUserLanguagePreferences>());
        Services.AddSingleton<IBargeInCoordinator>(new BargeInCoordinator());
        Services.AddSingleton(Mock.Of<ITextToSpeechPlaybackState>());
        Services.AddSingleton(Mock.Of<IMicGateState>());
        var pcm = new Mock<IPcmAudioPlayer>();
        pcm.Setup(p => p.InitializeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        pcm.Setup(p => p.ResetAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        pcm.Setup(p => p.SetMakeupAsync(It.IsAny<double>(), It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        Services.AddSingleton(pcm.Object);
        var earcon = new Mock<IEarconPlayer>();
        earcon.Setup(p => p.StopLoopingAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        earcon.Setup(p => p.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        Services.AddSingleton(earcon.Object);
        var calls = 0;
        var disposed = 0;
        var reader = new Mock<IAsyncStreamReader<AudioResponse>>(MockBehavior.Strict);
        reader.Setup(r => r.MoveNext(It.IsAny<CancellationToken>())).Returns(async (CancellationToken token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return false;
        });
        var grpc = new Mock<Sprechen.Proto.Speech.SpeechClient>(MockBehavior.Strict, new Mock<CallInvoker>().Object);
        grpc.Setup(c => c.TextToSpeechToStream(It.IsAny<TextRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                Interlocked.Increment(ref calls);
                return new AsyncServerStreamingCall<AudioResponse>(reader.Object, Task.FromResult(new Metadata()),
                    () => Status.DefaultSuccess, () => new Metadata(), () => Interlocked.Increment(ref disposed));
            });
        Services.AddSingleton(grpc.Object);
        var failures = 0;
        using var subscription = messageBus.Subscribe<SpeechCaptureFailed>(_ =>
        {
            Interlocked.Increment(ref failures);
            return ValueTask.CompletedTask;
        });
        using var transcript = Render<SpeechCaptureTextToSpeechAnnouncer>();
        using var errorCue = Render<SpeechCaptureErrorEarconSubscriber>();
        using var alert = Render<SpeechCaptureFailureAnnouncer>();

        await messageBus.PublishAsync(new SpeechCaptureStopped("de", "Synthetic transcript"));
        await settled.Task.WaitAsync(TimeSpan.FromSeconds(5));

        alert.WaitForAssertion(() =>
        {
            Assert.Equal(2, loggedErrors.Count);
            Assert.All(loggedErrors, error => Assert.IsType<SpeechErrorException>(error));
            Assert.Equal(2, Volatile.Read(ref calls));
            Assert.Equal(2, Volatile.Read(ref disposed));
            Assert.Equal(1, Volatile.Read(ref failures));
            earcon.Verify(p => p.PlayAsync(EarconId.ValidationAsrFail, It.IsAny<CancellationToken>()), Times.Exactly(3));
            Assert.False(string.IsNullOrWhiteSpace(alert.Find("[role=alert]").TextContent));
        }, TimeSpan.FromSeconds(5));
    }

    private RecordingLogger<SpeechCaptureTextToSpeechAnnouncer> RegisterLogger()
    {
        var logger = new RecordingLogger<SpeechCaptureTextToSpeechAnnouncer>();
        Services.AddSingleton<ILogger<SpeechCaptureTextToSpeechAnnouncer>>(_ => logger);
        return logger;
    }

    private void RegisterLanguageState(string isoCode)
    {
        var state = new UserLanguageState();
        state.SetSelectedIsoCode(isoCode);
        Services.AddSingleton<IUserLanguageState>(state);
    }

    private static string ResolveResourceText(string resourceId, string isoCode)
    {
        Assert.True(VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, isoCode, out var text));
        return text;
    }
}
