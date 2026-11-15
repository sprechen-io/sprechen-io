using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1.VoiceInterfaces;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft das Zusammensetzen von Okay und Hilfe aus zwei Aufnahmen zur Aktivierungsphrase.
/// Innerhalb des Zeitfensters entsteht eine Aktivierung ohne verbleibenden Befehl.
/// Nach Ablauf des Fensters oder bei einem unpassenden zweiten Transkript folgt die
/// Fehlerquittierung. Der zunächst gespeicherte Präfix allein löst keine Quittierung aus.
/// </summary>
public sealed class WakeUtteranceStitchingAcrossCyclesTests
{
    [Fact]
    public async Task ExecuteAsync_OkayThenHilfeAcrossTwoCycles_InWindow_StitchesIntoSingleDetectedEventAndStaysSilent()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var utterance1 = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1 }]);
        var utterance2 = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 2 }]);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance1));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance2));
        speechClient.EnqueueTranscript("Okay", confidencePercent: 80);
        speechClient.EnqueueTranscript("Hilfe", confidencePercent: 94);

        var window = new WakeUtteranceWindow
        {
            MaxInterCyclePauseMs = 1500,
            MaxCarriedCycles = 1,
            PrefixOnlyFirstCycleAccepted = true
        };
        var wakeWordProvider = new FixedWakeWordProvider(["okay hilfe"], EarconId.WakeWordNotRecognized, window);
        var trace = new RecordingSpeechAssistantTrace();
        var earconPlayer = new RecordingEarconPlayer();
        var context = CreateContext(speechClient, wakeWordProvider, trace, earconPlayer);

        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), It.IsAny<DispatchMode>(), It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, _, _) =>
            {
                lock (dispatchedEvents)
                {
                    dispatchedEvents.Add(@event);
                }
            })
            .Returns(ValueTask.CompletedTask);

        using var cts = new CancellationTokenSource();
        var effect = new ListenForWakeWordEffect("de");
        var executeTask = effect.ExecuteAsync(context, dispatcher.Object, cts.Token).AsTask();

        var waitDeadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);
        while (executeTask.Status == TaskStatus.WaitingForActivation
            && !executeTask.IsCompleted
            && DateTimeOffset.UtcNow < waitDeadline)
        {
            SpeechAssistantWakeWordDetectedEvent? detected;
            lock (dispatchedEvents)
            {
                detected = dispatchedEvents.OfType<SpeechAssistantWakeWordDetectedEvent>().FirstOrDefault();
            }

            if (detected is not null)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }

        cts.Cancel();
        try
        {
            await executeTask;
        }
        catch (OperationCanceledException)
        {
        }

        var detectedEvents = dispatchedEvents.OfType<SpeechAssistantWakeWordDetectedEvent>().ToArray();
        Assert.Single(detectedEvents);
        Assert.Equal("Okay Hilfe", detectedEvents[0].Transcript);
        Assert.Equal(string.Empty, detectedEvents[0].CommandText);

        Assert.Empty(dispatchedEvents.OfType<SpeechAssistantWakeWordMissedEvent>());
        Assert.Empty(earconPlayer.PlayedEarcons);
    }

    [Fact]
    public async Task ExecuteAsync_OkayThenHilfeAcrossTwoCycles_OutOfWindow_FallsThroughToWakeMiss()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var utterance1 = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1 }]);
        var utterance2 = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 2 }]);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance1));
        await speechClient.EnqueueRecordStepAsync(async ct =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), ct);
            return utterance2;
        });
        speechClient.EnqueueTranscript("Okay", confidencePercent: 80);
        speechClient.EnqueueTranscript("Hilfe", confidencePercent: 94);

        var window = new WakeUtteranceWindow
        {
            MaxInterCyclePauseMs = 1,
            MaxCarriedCycles = 1,
            PrefixOnlyFirstCycleAccepted = true
        };
        var wakeWordProvider = new FixedWakeWordProvider(["okay hilfe"], EarconId.WakeWordNotRecognized, window);
        var trace = new RecordingSpeechAssistantTrace();
        var earconPlayer = new RecordingEarconPlayer();
        var context = CreateContext(speechClient, wakeWordProvider, trace, earconPlayer);

        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), It.IsAny<DispatchMode>(), It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, _, _) =>
            {
                lock (dispatchedEvents)
                {
                    dispatchedEvents.Add(@event);
                }
            })
            .Returns(ValueTask.CompletedTask);

        using var cts = new CancellationTokenSource();
        var effect = new ListenForWakeWordEffect("de");
        var executeTask = effect.ExecuteAsync(context, dispatcher.Object, cts.Token).AsTask();

        var waitDeadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);
        while (speechClient.SpeechToTextCallCount < 2 && DateTimeOffset.UtcNow < waitDeadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }

        // Vor dem Abbruch bleibt kurz Zeit, den zweiten Erkennungsfehlschlag zu verarbeiten.
        await Task.Delay(TimeSpan.FromMilliseconds(50));

        cts.Cancel();
        try
        {
            await executeTask;
        }
        catch (OperationCanceledException)
        {
        }

        Assert.DoesNotContain(dispatchedEvents, @event => @event is SpeechAssistantWakeWordDetectedEvent);

        var missEvents = dispatchedEvents.OfType<SpeechAssistantWakeWordMissedEvent>().ToArray();
        Assert.Single(missEvents);
        Assert.Equal(nameof(WakeWordActivationStatus.MissingLeadingWakeWord), missEvents[0].Status);
        Assert.Contains(
            trace.Entries,
            entry => entry.Scope == SpeechAssistantTraceScope.PostWakeWordRecovery
                && entry.Message == "wake stitch expired previousPrefixLen=4");

        Assert.Single(earconPlayer.PlayedEarcons);
        Assert.Equal(EarconId.WakeWordNotRecognized, earconPlayer.PlayedEarcons[0]);
    }

    [Fact]
    public async Task ExecuteAsync_OkayThenUnrelatedTranscriptAcrossTwoCycles_EmitsNoOpTelemetryAndFallsThroughToWakeMiss()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var utterance1 = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1 }]);
        var utterance2 = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 2 }]);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance1));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance2));
        speechClient.EnqueueTranscript("Okay", confidencePercent: 80);
        speechClient.EnqueueTranscript("Thema", confidencePercent: 41);

        var window = new WakeUtteranceWindow
        {
            MaxInterCyclePauseMs = 1500,
            MaxCarriedCycles = 1,
            PrefixOnlyFirstCycleAccepted = true
        };
        var wakeWordProvider = new FixedWakeWordProvider(["okay hilfe"], EarconId.WakeWordNotRecognized, window);
        var trace = new RecordingSpeechAssistantTrace();
        var earconPlayer = new RecordingEarconPlayer();
        var context = CreateContext(speechClient, wakeWordProvider, trace, earconPlayer);

        var dispatchedEvents = new List<SpeechAssistantEvent>();
        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), It.IsAny<DispatchMode>(), It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, _, _) =>
            {
                lock (dispatchedEvents)
                {
                    dispatchedEvents.Add(@event);
                }
            })
            .Returns(ValueTask.CompletedTask);

        using var cts = new CancellationTokenSource();
        var effect = new ListenForWakeWordEffect("de");
        var executeTask = effect.ExecuteAsync(context, dispatcher.Object, cts.Token).AsTask();

        var waitDeadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);
        while (speechClient.SpeechToTextCallCount < 2 && DateTimeOffset.UtcNow < waitDeadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }

        await Task.Delay(TimeSpan.FromMilliseconds(50));

        cts.Cancel();
        try
        {
            await executeTask;
        }
        catch (OperationCanceledException)
        {
        }

        Assert.DoesNotContain(dispatchedEvents, @event => @event is SpeechAssistantWakeWordDetectedEvent);
        Assert.Single(dispatchedEvents.OfType<SpeechAssistantWakeWordMissedEvent>());
        Assert.Contains(
            trace.Entries,
            entry => entry.Scope == SpeechAssistantTraceScope.PostWakeWordRecovery
                && entry.Message == "wake stitch noop transcriptLen=5");
        Assert.Single(earconPlayer.PlayedEarcons);
        Assert.Equal(EarconId.WakeWordNotRecognized, earconPlayer.PlayedEarcons[0]);
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
}
