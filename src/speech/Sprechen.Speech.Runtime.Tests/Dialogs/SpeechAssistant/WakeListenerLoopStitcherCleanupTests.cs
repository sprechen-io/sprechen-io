using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1.VoiceInterfaces;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class WakeListenerLoopStitcherCleanupTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCancelledAfterStartedStitch_FollowUpRunDoesNotReusePriorCarry()
    {
        var wakeWordProvider = CreateTwoCycleWakeWordProvider();

        var firstSpeechClient = new ScriptedSpeechInputClient();
        await firstSpeechClient.EnqueueRecordStepAsync(_ => Task.FromResult(CreateUtterance(1)));
        await firstSpeechClient.EnqueueRecordStepAsync(async ct =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return CreateUtterance(9);
        });
        firstSpeechClient.EnqueueTranscript("Okay", confidencePercent: 80);

        var firstDispatchedEvents = new List<SpeechAssistantEvent>();

        await RunUntilCancellationAsync(
            firstSpeechClient,
            wakeWordProvider,
            firstDispatchedEvents,
            earconPlayer: new NoOpEarconPlayer(),
            waitCondition: () => firstSpeechClient.RecordCallCount >= 2);

        Assert.DoesNotContain(firstDispatchedEvents, @event => @event is SpeechAssistantWakeWordDetectedEvent);

        var secondSpeechClient = new ScriptedSpeechInputClient();
        await secondSpeechClient.EnqueueRecordStepAsync(_ => Task.FromResult(CreateUtterance(2)));
        await secondSpeechClient.EnqueueRecordStepAsync(async ct =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return CreateUtterance(3);
        });
        secondSpeechClient.EnqueueTranscript("Hilfe", confidencePercent: 94);

        var secondDispatchedEvents = new List<SpeechAssistantEvent>();
        var secondEarconPlayer = new RecordingEarconPlayer();

        await RunUntilCancellationAsync(
            secondSpeechClient,
            wakeWordProvider,
            secondDispatchedEvents,
            secondEarconPlayer,
            waitCondition: () => secondSpeechClient.RecordCallCount >= 2);

        Assert.DoesNotContain(secondDispatchedEvents, @event => @event is SpeechAssistantWakeWordDetectedEvent);
        Assert.Single(secondDispatchedEvents.OfType<SpeechAssistantWakeWordMissedEvent>());
        Assert.Equal(EarconId.WakeWordNotRecognized, Assert.Single(secondEarconPlayer.PlayedEarcons));
    }

    [Fact]
    public async Task ExecuteAsync_WhenWakeMissEarconThrowsAfterNoOp_ThirdCycleDoesNotReuseStartedCarry()
    {
        var wakeWordProvider = CreateTwoCycleWakeWordProvider();
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(CreateUtterance(1)));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(CreateUtterance(2)));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(CreateUtterance(3)));
        await speechClient.EnqueueRecordStepAsync(async ct =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return CreateUtterance(4);
        });
        speechClient.EnqueueTranscript("Okay", confidencePercent: 80);
        speechClient.EnqueueTranscript("Thema", confidencePercent: 41);
        speechClient.EnqueueTranscript("Hilfe", confidencePercent: 94);

        var playAttempt = 0;
        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer
            .Setup(player => player.PlayAsync(EarconId.WakeWordNotRecognized, It.IsAny<CancellationToken>()))
            .Returns<EarconId, CancellationToken>((_, _) =>
            {
                playAttempt++;
                return playAttempt == 1
                    ? ValueTask.FromException(new InvalidOperationException("boom"))
                    : ValueTask.CompletedTask;
            });

        var dispatchedEvents = new List<SpeechAssistantEvent>();

        var trace = await RunUntilCancellationAsync(
            speechClient,
            wakeWordProvider,
            dispatchedEvents,
            earconPlayer.Object,
            waitCondition: () => speechClient.RecordCallCount >= 4);

        Assert.DoesNotContain(dispatchedEvents, @event => @event is SpeechAssistantWakeWordDetectedEvent);
        var missEvents = dispatchedEvents.OfType<SpeechAssistantWakeWordMissedEvent>().ToArray();
        Assert.Equal(2, missEvents.Length);
        Assert.Contains(
            trace.Entries,
            entry => entry.Scope == SpeechAssistantTraceScope.PostWakeWordRecovery
                && entry.Message == "wake stitch noop transcriptLen=5");
        Assert.Contains(
            trace.Entries,
            entry => entry.Scope == SpeechAssistantTraceScope.WakeWordEffect
                && entry.Level == SpeechAssistantTraceLevel.Warning
                && entry.Message == "Wake-word miss earcon playback failed; continuing listener loop.");
    }

    [Fact]
    public async Task ExecuteAsync_WhenWakeMissEarconPlaybackIsPending_DoesNotArmNextActivationListeningUntilPlaybackCompletes()
    {
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(CreateUtterance(1)));
        await speechClient.EnqueueRecordStepAsync(async ct =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return CreateUtterance(2);
        });
        speechClient.EnqueueTranscript("Thema", confidencePercent: 38);

        var wakeWordProvider = new FixedWakeWordProvider(["Okay"], EarconId.WakeWordNotRecognized);
        var playbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releasePlayback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer
            .Setup(player => player.PlayAsync(EarconId.WakeWordNotRecognized, It.IsAny<CancellationToken>()))
            .Returns<EarconId, CancellationToken>(async (_, cancellationToken) =>
            {
                playbackStarted.TrySetResult();
                await releasePlayback.Task.WaitAsync(cancellationToken);
            });

        var trace = new RecordingSpeechAssistantTrace();
        var context = CreateContext(speechClient, wakeWordProvider, trace, earconPlayer.Object);
        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = CreateRecordingDispatcher(dispatchedEvents);

        using var cts = new CancellationTokenSource();
        var effect = new ListenForWakeWordEffect("de");

        var executeTask = effect.ExecuteAsync(context, dispatcher.Object, cts.Token).AsTask();

        await playbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, speechClient.RecordCallCount);
        Assert.Equal([SpeechCaptureMode.ActivationListening], speechClient.RecordModes);

        releasePlayback.TrySetResult();
        await WaitUntilAsync(() => speechClient.RecordCallCount >= 2);

        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executeTask);

        Assert.Single(dispatchedEvents.OfType<SpeechAssistantWakeWordMissedEvent>());
        Assert.Contains(
            trace.Entries,
            entry => entry.Scope == SpeechAssistantTraceScope.WakeWordEffect
                && entry.Message == "Wake-word transcription did not match the modeled inventory. confidence=38");
    }

    private static async Task<RecordingSpeechAssistantTrace> RunUntilCancellationAsync(
        ScriptedSpeechInputClient speechClient,
        IWakeWordProvider wakeWordProvider,
        List<SpeechAssistantEvent> dispatchedEvents,
        IEarconPlayer earconPlayer,
        Func<bool> waitCondition)
    {
        ArgumentNullException.ThrowIfNull(speechClient);
        ArgumentNullException.ThrowIfNull(wakeWordProvider);
        ArgumentNullException.ThrowIfNull(dispatchedEvents);
        ArgumentNullException.ThrowIfNull(earconPlayer);
        ArgumentNullException.ThrowIfNull(waitCondition);

        var trace = new RecordingSpeechAssistantTrace();
        var context = CreateContext(speechClient, wakeWordProvider, trace, earconPlayer);
        var dispatcher = CreateRecordingDispatcher(dispatchedEvents);

        using var cts = new CancellationTokenSource();
        var effect = new ListenForWakeWordEffect("de");
        var executeTask = effect.ExecuteAsync(context, dispatcher.Object, cts.Token).AsTask();

        await WaitUntilAsync(waitCondition);

        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executeTask);
        return trace;
    }

    private static Mock<IEventDispatcher<SpeechAssistantEvent>> CreateRecordingDispatcher(
        List<SpeechAssistantEvent> sink)
    {
        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), It.IsAny<DispatchMode>(), It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, _, _) =>
            {
                lock (sink)
                {
                    sink.Add(@event);
                }
            })
            .Returns(ValueTask.CompletedTask);
        return dispatcher;
    }

    private static SpeechAssistantContext CreateContext(
        ScriptedSpeechInputClient speechClient,
        IWakeWordProvider wakeWordProvider,
        ISpeechAssistantTrace trace,
        IEarconPlayer earconPlayer)
    {
        return SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            speechClient,
            speechClient,
            new NoOpTextToSpeechPlayer(),
            earconPlayer,
            wakeWordProvider,
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            new NoOpVoiceCommandDialog(),
            NullLogger<SpeechAssistantService>.Instance,
            speechAssistantTrace: trace);
    }

    private static CapturedSpeechAudio CreateUtterance(byte value)
        => new(SpeechAudioMimeType.Pcm, [new byte[] { value }]);

    private static FixedWakeWordProvider CreateTwoCycleWakeWordProvider()
    {
        var window = new WakeUtteranceWindow
        {
            MaxInterCyclePauseMs = 1500,
            MaxCarriedCycles = 1,
            PrefixOnlyFirstCycleAccepted = true
        };

        return new FixedWakeWordProvider(["okay hilfe"], EarconId.WakeWordNotRecognized, window);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException("Condition was not met before the timeout.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }
    }
}
