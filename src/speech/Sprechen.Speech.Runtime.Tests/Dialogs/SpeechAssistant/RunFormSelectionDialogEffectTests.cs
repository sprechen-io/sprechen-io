using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Immutable;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class RunFormSelectionDialogEffectTests
{
    [Fact]
    public async Task ExecuteAsync_WhenDialogThrows_PublishesFailureAndCancelsSelection()
    {
        var formSelectionDialog = new Mock<IFormSelectionDialog>(MockBehavior.Strict);
        formSelectionDialog
            .Setup(dialog => dialog.ExecuteAsync(
                "de",
                "app.alpha",
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus
            .Setup(bus => bus.PublishAsync(It.Is<SpeechCaptureFailed>(message =>
                message.ErrorCode == SpeechErrorCode.FormSelectionDialogFailed
                && message.Message == "Form selection dialog failed.")))
            .Returns(ValueTask.CompletedTask);

        SpeechAssistantEvent? dispatchedEvent = null;
        var dispatcher = CreateDispatcher(@event => dispatchedEvent = @event);

        var effect = new RunFormSelectionDialogEffect(
            "de",
            "app.alpha",
            ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase));
        await effect.ExecuteAsync(CreateContext(formSelectionDialog.Object, messageBus.Object), dispatcher.Object, CancellationToken.None);

        Assert.IsType<SpeechAssistantFormSelectionCancelledEvent>(dispatchedEvent);
        formSelectionDialog.VerifyAll();
        messageBus.VerifyAll();
        dispatcher.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenApplicationIdIsMissing_PublishesFailureAndCancelsSelection()
    {
        var formSelectionDialog = new Mock<IFormSelectionDialog>(MockBehavior.Strict);
        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus
            .Setup(bus => bus.PublishAsync(It.Is<SpeechCaptureFailed>(message =>
                message.ErrorCode == SpeechErrorCode.FormSelectionDialogFailed
                && message.Message == "Form selection dialog failed.")))
            .Returns(ValueTask.CompletedTask);

        SpeechAssistantEvent? dispatchedEvent = null;
        var dispatcher = CreateDispatcher(@event => dispatchedEvent = @event);

        var effect = new RunFormSelectionDialogEffect(
            "de",
            selectedApplicationId: null,
            ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase));
        await effect.ExecuteAsync(CreateContext(formSelectionDialog.Object, messageBus.Object), dispatcher.Object, CancellationToken.None);

        Assert.IsType<SpeechAssistantFormSelectionCancelledEvent>(dispatchedEvent);
        formSelectionDialog.VerifyNoOtherCalls();
        messageBus.VerifyAll();
        dispatcher.VerifyAll();
    }

    private static Mock<IEventDispatcher<SpeechAssistantEvent>> CreateDispatcher(Action<SpeechAssistantEvent> onDispatched)
    {
        ArgumentNullException.ThrowIfNull(onDispatched);

        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), DispatchMode.Enqueue, It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, _, _) => onDispatched(@event))
            .Returns(ValueTask.CompletedTask);
        return dispatcher;
    }

    private static SpeechAssistantContext CreateContext(
        IFormSelectionDialog formSelectionDialog,
        IClientMessageBus? messageBus = null)
    {
        ArgumentNullException.ThrowIfNull(formSelectionDialog);

        return SpeechAssistantContextTestFactory.Create(
            messageBus is null ? new NoOpClientMessageBus() : SpeechInteractionSignalsTestFactory.Create(messageBus),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            Mock.Of<ISpeechCaptureClient>(),
            null,
            Mock.Of<ISpeechRecognitionClient>(),
            Mock.Of<ITextToSpeechPlayer>(),
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            formSelectionDialog,
            new NoOpVoiceCommandDialog(),
            NullLogger<SpeechAssistantService>.Instance);
    }
}
