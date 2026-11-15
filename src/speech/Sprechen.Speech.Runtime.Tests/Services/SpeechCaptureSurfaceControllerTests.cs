using Sprechen.Proto;
using Microsoft.Extensions.Logging;
using Moq;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechCaptureSurfaceControllerTests
{
    [Fact]
    public async Task HandlePressAsync_WhenAssistantNotStarted_StartsAssistantAndPublishesReadyWithoutToggling()
    {
        var assistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        var feedback = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var logger = new RecordingLogger<SpeechCaptureSurfaceController>();
        var activationStates = new List<SpeechAssistantActivationState>();

        using var activationSubscription = messageBus.Subscribe<SpeechAssistantActivationStateChanged>(message =>
        {
            activationStates.Add(message.State);
            return ValueTask.CompletedTask;
        });

        assistant
            .Setup(service => service.StartAsync(CancellationToken.None))
            .Returns(ValueTask.FromResult(SpeechAssistantStartOutcome.SessionStarted));

        using var controller = new SpeechCaptureSurfaceController(assistant.Object, feedback, new ControlledMicrophoneRecorder(), new RecordingEarconPlayer(), new RecordingPcmAudioPlayer(), logger);

        await controller.HandlePressAsync(CancellationToken.None);

        assistant.Verify(service => service.StartAsync(CancellationToken.None), Times.Once);
        assistant.Verify(service => service.ToggleVoiceInputAsync(CancellationToken.None), Times.Never);
        Assert.Equal(
            [SpeechAssistantActivationState.Starting, SpeechAssistantActivationState.Ready],
            activationStates);
    }

    // Nach abgebrochenem Einstieg muss der nächste Tastendruck erneut starten, statt eine Sitzung umzuschalten.
    [Fact]
    public async Task HandlePressAsync_WhenTheEntryWasCancelled_StaysNotStartedAndTheNextPressStartsAgain()
    {
        var assistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        var feedback = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var logger = new RecordingLogger<SpeechCaptureSurfaceController>();
        var activationStates = new List<SpeechAssistantActivationState>();

        using var activationSubscription = messageBus.Subscribe<SpeechAssistantActivationStateChanged>(message =>
        {
            activationStates.Add(message.State);
            return ValueTask.CompletedTask;
        });

        assistant
            .SetupSequence(service => service.StartAsync(CancellationToken.None))
            .Returns(ValueTask.FromResult(SpeechAssistantStartOutcome.CancelledAtEntry))
            .Returns(ValueTask.FromResult(SpeechAssistantStartOutcome.SessionStarted));

        using var controller = new SpeechCaptureSurfaceController(assistant.Object, feedback, new ControlledMicrophoneRecorder(), new RecordingEarconPlayer(), new RecordingPcmAudioPlayer(), logger);

        await controller.HandlePressAsync(CancellationToken.None);
        await controller.HandlePressAsync(CancellationToken.None);

        assistant.Verify(service => service.StartAsync(CancellationToken.None), Times.Exactly(2));
        assistant.Verify(service => service.ToggleVoiceInputAsync(CancellationToken.None), Times.Never);
        Assert.Equal(
            [
                SpeechAssistantActivationState.Starting,
                SpeechAssistantActivationState.NotStarted,
                SpeechAssistantActivationState.Starting,
                SpeechAssistantActivationState.Ready,
            ],
            activationStates);
    }

    [Fact]
    public async Task HandlePressAsync_OnFirstStart_WarmsEarconEndpointExactlyOnceAcrossPresses()
    {
        // Earconausgabe beim ersten Start vorbereiten und nach erfolgreicher Vorbereitung nicht bei jedem Tastendruck wiederholen.
        var assistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        var feedback = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var logger = new RecordingLogger<SpeechCaptureSurfaceController>();
        var earconPlayer = new RecordingEarconPlayer();

        assistant
            .Setup(service => service.StartAsync(CancellationToken.None))
            .Returns(ValueTask.FromResult(SpeechAssistantStartOutcome.SessionStarted));
        assistant
            .Setup(service => service.ToggleVoiceInputAsync(CancellationToken.None))
            .Returns(ValueTask.FromResult(SpeechAssistantActivationState.Ready));

        using var controller = new SpeechCaptureSurfaceController(assistant.Object, feedback, new ControlledMicrophoneRecorder(), earconPlayer, new RecordingPcmAudioPlayer(), logger);

        await controller.HandlePressAsync(CancellationToken.None);
        await controller.HandlePressAsync(CancellationToken.None);

        Assert.Equal(1, earconPlayer.WarmupCount);
    }

    [Fact]
    public async Task HandlePressAsync_OnFirstStart_WarmsTtsOutputEndpointExactlyOnceAcrossPresses()
    {
        // Auch die TTS-Ausgabe wird beim ersten Start vorbereitet; erfolgreiches Warmup soll über weitere Tastendrücke erhalten bleiben.
        var assistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        var feedback = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var logger = new RecordingLogger<SpeechCaptureSurfaceController>();
        var pcmAudioPlayer = new RecordingPcmAudioPlayer();

        assistant
            .Setup(service => service.StartAsync(CancellationToken.None))
            .Returns(ValueTask.FromResult(SpeechAssistantStartOutcome.SessionStarted));
        assistant
            .Setup(service => service.ToggleVoiceInputAsync(CancellationToken.None))
            .Returns(ValueTask.FromResult(SpeechAssistantActivationState.Ready));

        using var controller = new SpeechCaptureSurfaceController(assistant.Object, feedback, new ControlledMicrophoneRecorder(), new RecordingEarconPlayer(), pcmAudioPlayer, logger);

        await controller.HandlePressAsync(CancellationToken.None);
        await controller.HandlePressAsync(CancellationToken.None);

        Assert.Equal(1, pcmAudioPlayer.WarmupCount);
    }

    [Fact]
    public async Task HandlePressAsync_WhenTtsWarmupFails_PublishesFailedAndRetriesBeforeReady()
    {
        // Fehlgeschlagenes Warmup verhindert Ready und muss beim nächsten Startversuch erneut ausgeführt werden.
        var assistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var pcmAudioPlayer = new Mock<IPcmAudioPlayer>(MockBehavior.Strict);
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        var feedback = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var logger = new RecordingLogger<SpeechCaptureSurfaceController>();
        var activationStates = new List<SpeechAssistantActivationState>();
        var failures = new List<SpeechCaptureFailed>();

        using var activationSubscription = messageBus.Subscribe<SpeechAssistantActivationStateChanged>(message =>
        {
            activationStates.Add(message.State);
            return ValueTask.CompletedTask;
        });
        using var failureSubscription = messageBus.Subscribe<SpeechCaptureFailed>(message =>
        {
            failures.Add(message);
            return ValueTask.CompletedTask;
        });

        var warmupFailure = new SpeechErrorException(
            SpeechErrorCode.ProviderFailure,
            "NotAllowedError while keep-alive floor was unavailable");
        pcmAudioPlayer
            .SetupSequence(player => player.WarmupAsync(CancellationToken.None))
            .Returns(new ValueTask(Task.FromException(warmupFailure)))
            .Returns(ValueTask.CompletedTask);
        assistant
            .Setup(service => service.StartAsync(CancellationToken.None))
            .Returns(ValueTask.FromResult(SpeechAssistantStartOutcome.SessionStarted));

        using var controller = new SpeechCaptureSurfaceController(
            assistant.Object,
            feedback,
            new ControlledMicrophoneRecorder(),
            new RecordingEarconPlayer(),
            pcmAudioPlayer.Object,
            logger);

        await controller.HandlePressAsync(CancellationToken.None);
        await controller.HandlePressAsync(CancellationToken.None);

        pcmAudioPlayer.Verify(player => player.WarmupAsync(CancellationToken.None), Times.Exactly(2));
        assistant.Verify(service => service.StartAsync(CancellationToken.None), Times.Once);
        assistant.Verify(service => service.ToggleVoiceInputAsync(CancellationToken.None), Times.Never);
        Assert.Equal(
            [
                SpeechAssistantActivationState.Starting,
                SpeechAssistantActivationState.Failed,
                SpeechAssistantActivationState.Starting,
                SpeechAssistantActivationState.Ready
            ],
            activationStates);
        var failure = Assert.Single(failures);
        Assert.Equal(
            SpeechErrorCode.Unspecified,
            failure.ErrorCode); // Allgemeiner Startfehler statt eines Fehlers während laufender Erkennung.
        Assert.Contains("NotAllowedError", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandlePressAsync_WhenAssistantAlreadyStarted_HandlesPressWithoutRepublishingActivationState()
    {
        var assistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        var feedback = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var logger = new RecordingLogger<SpeechCaptureSurfaceController>();
        var activationStates = new List<SpeechAssistantActivationState>();

        using var activationSubscription = messageBus.Subscribe<SpeechAssistantActivationStateChanged>(message =>
        {
            activationStates.Add(message.State);
            return ValueTask.CompletedTask;
        });

        assistant
            .Setup(service => service.StartAsync(CancellationToken.None))
            .Returns(ValueTask.FromResult(SpeechAssistantStartOutcome.SessionStarted));
        assistant
            .Setup(service => service.ToggleVoiceInputAsync(CancellationToken.None))
            .Returns(ValueTask.FromResult(SpeechAssistantActivationState.Ready));

        using var controller = new SpeechCaptureSurfaceController(assistant.Object, feedback, new ControlledMicrophoneRecorder(), new RecordingEarconPlayer(), new RecordingPcmAudioPlayer(), logger);

        await controller.HandlePressAsync(CancellationToken.None);
        await controller.HandlePressAsync(CancellationToken.None);

        assistant.Verify(service => service.StartAsync(CancellationToken.None), Times.Once);
        assistant.Verify(service => service.ToggleVoiceInputAsync(CancellationToken.None), Times.Once);
        Assert.Equal(
            [SpeechAssistantActivationState.Starting, SpeechAssistantActivationState.Ready],
            activationStates);
    }

    [Fact]
    public async Task HandlePressAsync_WhenToggleReturnsPaused_PublishesPausedWithoutReadyOverwrite()
    {
        var assistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        var feedback = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var logger = new RecordingLogger<SpeechCaptureSurfaceController>();
        var activationStates = new List<SpeechAssistantActivationState>();
        var sequence = new MockSequence();

        using var activationSubscription = messageBus.Subscribe<SpeechAssistantActivationStateChanged>(message =>
        {
            activationStates.Add(message.State);
            return ValueTask.CompletedTask;
        });

        assistant.InSequence(sequence)
            .Setup(service => service.StartAsync(CancellationToken.None))
            .Returns(ValueTask.FromResult(SpeechAssistantStartOutcome.SessionStarted));
        assistant.InSequence(sequence)
            .Setup(service => service.ToggleVoiceInputAsync(CancellationToken.None))
            .Returns(ValueTask.FromResult(SpeechAssistantActivationState.Paused));

        using var controller = new SpeechCaptureSurfaceController(assistant.Object, feedback, new ControlledMicrophoneRecorder(), new RecordingEarconPlayer(), new RecordingPcmAudioPlayer(), logger);

        await controller.HandlePressAsync(CancellationToken.None);
        await controller.HandlePressAsync(CancellationToken.None);

        assistant.Verify(service => service.StartAsync(CancellationToken.None), Times.Once);
        assistant.Verify(service => service.ToggleVoiceInputAsync(CancellationToken.None), Times.Once);
        Assert.Equal(
            [
                SpeechAssistantActivationState.Starting,
                SpeechAssistantActivationState.Ready,
                SpeechAssistantActivationState.Paused
            ],
            activationStates);
    }

    [Fact]
    public async Task HandlePressAsync_WhenAssistantStartFails_PublishesFailureAndActivationFailed()
    {
        var assistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        var feedback = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var logger = new RecordingLogger<SpeechCaptureSurfaceController>();
        var activationStates = new List<SpeechAssistantActivationState>();
        SpeechCaptureFailed? publishedFailure = null;

        using var activationSubscription = messageBus.Subscribe<SpeechAssistantActivationStateChanged>(message =>
        {
            activationStates.Add(message.State);
            return ValueTask.CompletedTask;
        });
        using var failureSubscription = messageBus.Subscribe<SpeechCaptureFailed>(message =>
        {
            publishedFailure = message;
            return ValueTask.CompletedTask;
        });

        assistant
            .Setup(service => service.StartAsync(CancellationToken.None))
            .Returns(() => new ValueTask<SpeechAssistantStartOutcome>(Task.FromException<SpeechAssistantStartOutcome>(new InvalidOperationException("boom"))));

        var microphoneRecorder = new ControlledMicrophoneRecorder();
        using var controller = new SpeechCaptureSurfaceController(assistant.Object, feedback, microphoneRecorder, new RecordingEarconPlayer(), new RecordingPcmAudioPlayer(), logger);

        await controller.HandlePressAsync(CancellationToken.None);

        assistant.Verify(service => service.StartAsync(CancellationToken.None), Times.Once);
        assistant.Verify(service => service.ToggleVoiceInputAsync(CancellationToken.None), Times.Never);
        Assert.Equal(1, microphoneRecorder.ReleaseCallCount);
        Assert.Equal(
            [SpeechAssistantActivationState.Starting, SpeechAssistantActivationState.Failed],
            activationStates);
        Assert.NotNull(publishedFailure);
        Assert.Equal(SpeechErrorCode.Unspecified, publishedFailure.ErrorCode);
        Assert.Contains("boom", publishedFailure.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(InvalidOperationException), publishedFailure.Message, StringComparison.Ordinal);
        Assert.Contains(
            logger.Entries,
            entry => entry.Level == LogLevel.Error &&
                     entry.Message.Contains("Failed to initialize the speech assistant after a speech surface press.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandlePressAsync_WhenOnboardingPlaybackFails_PublishesFailedAndRetriesInsteadOfReady()
    {
        var assistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        var feedback = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var logger = new RecordingLogger<SpeechCaptureSurfaceController>();
        var activationStates = new List<SpeechAssistantActivationState>();
        var failures = new List<SpeechCaptureFailed>();

        using var activationSubscription = messageBus.Subscribe<SpeechAssistantActivationStateChanged>(message =>
        {
            activationStates.Add(message.State);
            return ValueTask.CompletedTask;
        });
        using var failureSubscription = messageBus.Subscribe<SpeechCaptureFailed>(message =>
        {
            failures.Add(message);
            return ValueTask.CompletedTask;
        });

        var playbackFailure = new SpeechErrorException(SpeechErrorCode.ProviderFailure, "output graph unavailable");
        assistant
            .SetupSequence(service => service.StartAsync(CancellationToken.None))
            .Returns(new ValueTask<SpeechAssistantStartOutcome>(Task.FromException<SpeechAssistantStartOutcome>(playbackFailure)))
            .Returns(ValueTask.FromResult(SpeechAssistantStartOutcome.SessionStarted));

        using var controller = new SpeechCaptureSurfaceController(
            assistant.Object,
            feedback,
            new ControlledMicrophoneRecorder(),
            new RecordingEarconPlayer(),
            new RecordingPcmAudioPlayer(),
            logger);

        await controller.HandlePressAsync(CancellationToken.None);
        await controller.HandlePressAsync(CancellationToken.None);

        assistant.Verify(service => service.StartAsync(CancellationToken.None), Times.Exactly(2));
        assistant.Verify(service => service.ToggleVoiceInputAsync(CancellationToken.None), Times.Never);
        Assert.Equal(
            [
                SpeechAssistantActivationState.Starting,
                SpeechAssistantActivationState.Failed,
                SpeechAssistantActivationState.Starting,
                SpeechAssistantActivationState.Ready
            ],
            activationStates);
        var failure = Assert.Single(failures);
        Assert.Equal(
            SpeechErrorCode.Unspecified,
            failure.ErrorCode); // Allgemeiner Startfehler statt eines Fehlers während laufender Erkennung.
        Assert.Contains("output graph unavailable", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandlePressAsync_WhenAssistantStartFails_ReleasesMicrophoneBeforeAnnouncingFailed()
    {
        var assistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        var feedback = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var logger = new RecordingLogger<SpeechCaptureSurfaceController>();
        var safetyOrdering = new List<string>();

        using var activationSubscription = messageBus.Subscribe<SpeechAssistantActivationStateChanged>(message =>
        {
            if (message.State == SpeechAssistantActivationState.Failed)
            {
                safetyOrdering.Add("announced-failed");
            }

            return ValueTask.CompletedTask;
        });

        assistant
            .Setup(service => service.StartAsync(CancellationToken.None))
            .Returns(() => new ValueTask<SpeechAssistantStartOutcome>(Task.FromException<SpeechAssistantStartOutcome>(new InvalidOperationException("boom"))));

        var microphoneRecorder = new ControlledMicrophoneRecorder(onReleaseInvoked: () => safetyOrdering.Add("released"));
        using var controller = new SpeechCaptureSurfaceController(assistant.Object, feedback, microphoneRecorder, new RecordingEarconPlayer(), new RecordingPcmAudioPlayer(), logger);

        await controller.HandlePressAsync(CancellationToken.None);

        Assert.Equal(["released", "announced-failed"], safetyOrdering);
    }

    [Fact]
    public async Task HandlePressAsync_WhenStartFailsWithPermissionDenied_ReportsMicrophonePermissionDeniedCode()
    {
        var assistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        var feedback = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var logger = new RecordingLogger<SpeechCaptureSurfaceController>();
        SpeechCaptureFailed? publishedFailure = null;

        using var failureSubscription = messageBus.Subscribe<SpeechCaptureFailed>(message =>
        {
            publishedFailure = message;
            return ValueTask.CompletedTask;
        });

        assistant
            .Setup(service => service.StartAsync(CancellationToken.None))
            .Returns(() => new ValueTask<SpeechAssistantStartOutcome>(Task.FromException<SpeechAssistantStartOutcome>(
                new MicrophoneAcquisitionException(MicrophoneAcquisitionFailureCode.PermissionDenied))));

        using var controller = new SpeechCaptureSurfaceController(assistant.Object, feedback, new ControlledMicrophoneRecorder(), new RecordingEarconPlayer(), new RecordingPcmAudioPlayer(), logger);

        await controller.HandlePressAsync(CancellationToken.None);

        Assert.NotNull(publishedFailure);
        Assert.Equal(SpeechErrorCode.MicrophonePermissionDenied, publishedFailure.ErrorCode);
    }

    [Fact]
    public async Task HandlePressAsync_WhenStartFailsWithDeviceMissing_ReportsMicrophoneUnavailableCode()
    {
        var assistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        var feedback = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var logger = new RecordingLogger<SpeechCaptureSurfaceController>();
        SpeechCaptureFailed? publishedFailure = null;

        using var failureSubscription = messageBus.Subscribe<SpeechCaptureFailed>(message =>
        {
            publishedFailure = message;
            return ValueTask.CompletedTask;
        });

        assistant
            .Setup(service => service.StartAsync(CancellationToken.None))
            .Returns(() => new ValueTask<SpeechAssistantStartOutcome>(Task.FromException<SpeechAssistantStartOutcome>(
                new MicrophoneAcquisitionException(MicrophoneAcquisitionFailureCode.DeviceNotFound))));

        using var controller = new SpeechCaptureSurfaceController(assistant.Object, feedback, new ControlledMicrophoneRecorder(), new RecordingEarconPlayer(), new RecordingPcmAudioPlayer(), logger);

        await controller.HandlePressAsync(CancellationToken.None);

        Assert.NotNull(publishedFailure);
        Assert.Equal(SpeechErrorCode.MicrophoneUnavailable, publishedFailure.ErrorCode);
    }

    [Theory]
    [InlineData("NotAllowedError: Permission denied")]
    [InlineData("NotFoundError: Requested device not found")]
    public async Task HandlePressAsync_WhenUntypedDiagnosticMentionsMicrophoneFailure_DoesNotGuessCode(string message)
    {
        var assistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        var feedback = new ClientMessageBusSpeechInteractionSignals(messageBus);
        SpeechCaptureFailed? publishedFailure = null;

        using var failureSubscription = messageBus.Subscribe<SpeechCaptureFailed>(failure =>
        {
            publishedFailure = failure;
            return ValueTask.CompletedTask;
        });

        assistant
            .Setup(service => service.StartAsync(CancellationToken.None))
            .Returns(() => new ValueTask<SpeechAssistantStartOutcome>(Task.FromException<SpeechAssistantStartOutcome>(
                new InvalidOperationException(message))));

        using var controller = new SpeechCaptureSurfaceController(
            assistant.Object,
            feedback,
            new ControlledMicrophoneRecorder(),
            new RecordingEarconPlayer(),
            new RecordingPcmAudioPlayer(),
            new RecordingLogger<SpeechCaptureSurfaceController>());

        await controller.HandlePressAsync(CancellationToken.None);

        Assert.NotNull(publishedFailure);
        Assert.Equal(SpeechErrorCode.Unspecified, publishedFailure.ErrorCode);
    }

    [Fact]
    public async Task HandlePressAsync_WhenPressHandlingFailsAfterSuccessfulStart_RestartsAssistantOnNextPress()
    {
        var assistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        var feedback = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var logger = new RecordingLogger<SpeechCaptureSurfaceController>();
        var activationStates = new List<SpeechAssistantActivationState>();
        var failures = new List<SpeechCaptureFailed>();
        var sequence = new MockSequence();

        using var activationSubscription = messageBus.Subscribe<SpeechAssistantActivationStateChanged>(message =>
        {
            activationStates.Add(message.State);
            return ValueTask.CompletedTask;
        });
        using var failureSubscription = messageBus.Subscribe<SpeechCaptureFailed>(message =>
        {
            failures.Add(message);
            return ValueTask.CompletedTask;
        });

        assistant.InSequence(sequence)
            .Setup(service => service.StartAsync(CancellationToken.None))
            .Returns(ValueTask.FromResult(SpeechAssistantStartOutcome.SessionStarted));
        assistant.InSequence(sequence)
            .Setup(service => service.ToggleVoiceInputAsync(CancellationToken.None))
            .Returns(() => new ValueTask<SpeechAssistantActivationState>(Task.FromException<SpeechAssistantActivationState>(new InvalidOperationException("press failed"))));
        assistant.InSequence(sequence)
            .Setup(service => service.StartAsync(CancellationToken.None))
            .Returns(ValueTask.FromResult(SpeechAssistantStartOutcome.SessionStarted));

        using var controller = new SpeechCaptureSurfaceController(assistant.Object, feedback, new ControlledMicrophoneRecorder(), new RecordingEarconPlayer(), new RecordingPcmAudioPlayer(), logger);

        await controller.HandlePressAsync(CancellationToken.None);
        await controller.HandlePressAsync(CancellationToken.None);
        await controller.HandlePressAsync(CancellationToken.None);

        assistant.Verify(service => service.StartAsync(CancellationToken.None), Times.Exactly(2));
        assistant.Verify(service => service.ToggleVoiceInputAsync(CancellationToken.None), Times.Once);
        Assert.Equal(
            [
                SpeechAssistantActivationState.Starting,
                SpeechAssistantActivationState.Ready,
                SpeechAssistantActivationState.Failed,
                SpeechAssistantActivationState.Starting,
                SpeechAssistantActivationState.Ready
            ],
            activationStates);
        Assert.Single(failures);
        Assert.Contains("press failed", failures[0].Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandlePressAsync_WhenControllerDisposedDuringPress_DoesNotThrowFromSemaphoreRelease()
    {
        var assistant = new Mock<ISpeechAssistantService>(MockBehavior.Strict);
        var messageBus = new ClientMessageBus(new RecordingLogger<ClientMessageBus>());
        var feedback = new ClientMessageBusSpeechInteractionSignals(messageBus);
        var logger = new RecordingLogger<SpeechCaptureSurfaceController>();
        var pressEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowPressCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        assistant
            .Setup(service => service.StartAsync(CancellationToken.None))
            .Returns(async () =>
            {
                pressEntered.TrySetResult();
                await allowPressCompletion.Task.ConfigureAwait(false);
                return SpeechAssistantStartOutcome.SessionStarted;
            });

        using var controller = new SpeechCaptureSurfaceController(assistant.Object, feedback, new ControlledMicrophoneRecorder(), new RecordingEarconPlayer(), new RecordingPcmAudioPlayer(), logger);

        var handleTask = controller.HandlePressAsync(CancellationToken.None).AsTask();
        await pressEntered.Task;

        controller.Dispose();
        allowPressCompletion.TrySetResult();

        await handleTask;

        assistant.Verify(service => service.StartAsync(CancellationToken.None), Times.Once);
        assistant.Verify(service => service.ToggleVoiceInputAsync(CancellationToken.None), Times.Never);
    }
}
