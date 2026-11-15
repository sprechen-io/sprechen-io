using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Immutable;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class RunVoiceCommandDialogEffectTests
{
    [Fact]
    public async Task ExecuteAsync_WhenIsoCodeIsMissing_PublishesFailureAndDispatchesAbandonedRecovery()
    {
        var voiceCommandDialog = new Mock<IVoiceCommandDialog>(MockBehavior.Strict);
        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus
            .Setup(bus => bus.PublishAsync(It.Is<SpeechCaptureFailed>(message =>
                message.ErrorCode == SpeechErrorCode.VoiceCommandDialogFailed
                && message.Message == "Voice command dialog failed.")))
            .Returns(ValueTask.CompletedTask);

        SpeechAssistantEvent? dispatchedEvent = null;
        var dispatcher = CreateDispatcher(@event => dispatchedEvent = @event);

        await new RunVoiceCommandDialogEffect(selectedIsoCode: null, selectedApplicationId: "test-app", SpeechAssistantApplicationSessionState.Empty)
            .ExecuteAsync(CreateContext(voiceCommandDialog.Object, messageBus.Object), dispatcher.Object, CancellationToken.None);

        voiceCommandDialog.VerifyNoOtherCalls();
        AssertVoiceCommandRecoveryEvent(dispatchedEvent, expectedApplicationId: "test-app");
        messageBus.VerifyAll();
        dispatcher.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenIsoCodeIsInvalid_PublishesFailureAndDispatchesAbandonedRecovery()
    {
        var voiceCommandDialog = new Mock<IVoiceCommandDialog>(MockBehavior.Strict);
        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus
            .Setup(bus => bus.PublishAsync(It.Is<SpeechCaptureFailed>(message =>
                message.ErrorCode == SpeechErrorCode.VoiceCommandDialogFailed
                && message.Message == "Voice command dialog failed.")))
            .Returns(ValueTask.CompletedTask);

        SpeechAssistantEvent? dispatchedEvent = null;
        var dispatcher = CreateDispatcher(@event => dispatchedEvent = @event);

        await new RunVoiceCommandDialogEffect(selectedIsoCode: "german", selectedApplicationId: "test-app", SpeechAssistantApplicationSessionState.Empty)
            .ExecuteAsync(CreateContext(voiceCommandDialog.Object, messageBus.Object), dispatcher.Object, CancellationToken.None);

        voiceCommandDialog.VerifyNoOtherCalls();
        AssertVoiceCommandRecoveryEvent(dispatchedEvent, expectedApplicationId: "test-app");
        messageBus.VerifyAll();
        dispatcher.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenApplicationIdIsMissing_PublishesFailureAndDispatchesContextFreeRecovery()
    {
        var voiceCommandDialog = new Mock<IVoiceCommandDialog>(MockBehavior.Strict);
        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus
            .Setup(bus => bus.PublishAsync(It.Is<SpeechCaptureFailed>(message =>
                message.ErrorCode == SpeechErrorCode.VoiceCommandDialogFailed
                && message.Message == "Voice command dialog failed.")))
            .Returns(ValueTask.CompletedTask);

        SpeechAssistantEvent? dispatchedEvent = null;
        var dispatcher = CreateDispatcher(@event => dispatchedEvent = @event);

        await new RunVoiceCommandDialogEffect(selectedIsoCode: "de", selectedApplicationId: null, SpeechAssistantApplicationSessionState.Empty)
            .ExecuteAsync(CreateContext(voiceCommandDialog.Object, messageBus.Object), dispatcher.Object, CancellationToken.None);

        voiceCommandDialog.VerifyNoOtherCalls();
        AssertVoiceCommandRecoveryEvent(dispatchedEvent, expectedApplicationId: null);
        messageBus.VerifyAll();
        dispatcher.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenIsoCodeIsValid_InvokesDialog()
    {
        var voiceCommandDialog = new Mock<IVoiceCommandDialog>(MockBehavior.Strict);
        voiceCommandDialog
            .Setup(dialog => dialog.ExecuteAsync(
                "de",
                "test-app",
                null,
                It.IsAny<SpeechAssistantApplicationSessionState>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "test-app",
                "test-form",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        var context = CreateContext(voiceCommandDialog.Object);
        SpeechAssistantEvent? dispatchedEvent = null;
        var dispatcher = CreateDispatcher(@event => dispatchedEvent = @event);

        await new RunVoiceCommandDialogEffect(selectedIsoCode: "de", selectedApplicationId: "test-app", SpeechAssistantApplicationSessionState.Empty)
            .ExecuteAsync(context, dispatcher.Object, CancellationToken.None);

        voiceCommandDialog.VerifyAll();
        var completedEvent = Assert.IsType<SpeechAssistantVoiceCommandCompletedEvent>(dispatchedEvent);
        Assert.True(completedEvent.Result.WasSubmitted);
        dispatcher.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenDialogThrows_PublishesFailure()
    {
        var voiceCommandDialog = new Mock<IVoiceCommandDialog>(MockBehavior.Strict);
        voiceCommandDialog
            .Setup(dialog => dialog.ExecuteAsync(
                "de",
                "test-app",
                null,
                It.IsAny<SpeechAssistantApplicationSessionState>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus
            .Setup(bus => bus.PublishAsync(It.Is<SpeechCaptureFailed>(message =>
                message.ErrorCode == SpeechErrorCode.VoiceCommandDialogFailed
                && message.Message == "Voice command dialog failed.")))
            .Returns(ValueTask.CompletedTask);

        var context = CreateContext(voiceCommandDialog.Object, messageBus.Object);
        SpeechAssistantEvent? dispatchedEvent = null;
        var dispatcher = CreateDispatcher(@event => dispatchedEvent = @event);

        await new RunVoiceCommandDialogEffect(selectedIsoCode: "de", selectedApplicationId: "test-app", SpeechAssistantApplicationSessionState.Empty)
            .ExecuteAsync(context, dispatcher.Object, CancellationToken.None);

        voiceCommandDialog.VerifyAll();
        AssertVoiceCommandRecoveryEvent(dispatchedEvent, expectedApplicationId: "test-app");
        messageBus.VerifyAll();
        dispatcher.VerifyAll();
    }

    private static void AssertVoiceCommandRecoveryEvent(
        SpeechAssistantEvent? dispatchedEvent,
        string? expectedApplicationId)
    {
        var completedEvent = Assert.IsType<SpeechAssistantVoiceCommandCompletedEvent>(dispatchedEvent);
        Assert.True(completedEvent.Result.WasAbandoned);
        Assert.Equal(expectedApplicationId, completedEvent.Result.ApplicationId);
    }

    private static Mock<IEventDispatcher<SpeechAssistantEvent>> CreateDispatcher(Action<SpeechAssistantEvent> onDispatched)
    {
        ArgumentNullException.ThrowIfNull(onDispatched);

        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(
                It.IsAny<SpeechAssistantEvent>(),
                DispatchMode.Enqueue,
                It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, _, _) => onDispatched(@event))
            .Returns(ValueTask.CompletedTask);
        return dispatcher;
    }

    private static SpeechAssistantContext CreateContext(
        IVoiceCommandDialog voiceCommandDialog,
        IClientMessageBus? messageBus = null)
    {
        ArgumentNullException.ThrowIfNull(voiceCommandDialog);

        return SpeechAssistantContextTestFactory.Create(
            messageBus is null ? new NoOpClientMessageBus() : SpeechInteractionSignalsTestFactory.Create(messageBus),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechInputClient>(),
            Mock.Of<ITextToSpeechPlayer>(),
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            voiceCommandDialog,
            NullLogger<SpeechAssistantService>.Instance);
    }
}

