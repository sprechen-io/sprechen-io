using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class RunFormsCatalogDialogEffectTests
{
    [Fact]
    public async Task ExecuteAsync_WhenInitialTranscriptProvided_PreservesConfidenceInSeededCatalogOverload()
    {
        var catalogDialog = new Mock<IFormsCatalogDialog>(MockBehavior.Strict);
        catalogDialog
            .Setup(dialog => dialog.ExecuteAsync(
                "de",
                It.IsAny<string>(),
                It.Is<SpeechTranscript>(transcript =>
                    transcript.Text == "Alpha" && transcript.ConfidencePercent == 93),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FormsCatalogDialogResult.Selected("app.alpha", "Alpha", 1, "form-alpha", "en"));

        SpeechAssistantEvent? dispatchedEvent = null;
        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<SpeechAssistantEvent>(), DispatchMode.Enqueue, It.IsAny<CancellationToken>()))
            .Callback<SpeechAssistantEvent, DispatchMode, CancellationToken>((@event, _, _) => dispatchedEvent = @event)
            .Returns(ValueTask.CompletedTask);

        var effect = new RunFormsCatalogDialogEffect("de", new SpeechTranscript("Alpha", ConfidencePercent: 93));
        await effect.ExecuteAsync(CreateContext(catalogDialog.Object), dispatcher.Object, CancellationToken.None);

        var selectedEvent = Assert.IsType<SpeechAssistantApplicationSelectedEvent>(dispatchedEvent);
        Assert.Equal("app.alpha", selectedEvent.ApplicationId);
        Assert.Equal("Alpha", selectedEvent.ApplicationName);
        Assert.Equal("en", selectedEvent.NormalizedEffectiveIsoCode);
        catalogDialog.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenDialogCancelsAfterLanguageChange_DispatchesEffectiveIsoCode()
    {
        var catalogDialog = new Mock<IFormsCatalogDialog>(MockBehavior.Strict);
        catalogDialog
            .Setup(dialog => dialog.ExecuteAsync("de", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FormsCatalogDialogResult.Cancelled(DialogCancellationReason.UserCancelled, "en"));

        SpeechAssistantEvent? dispatchedEvent = null;
        var dispatcher = CreateDispatcher(@event => dispatchedEvent = @event);

        var effect = new RunFormsCatalogDialogEffect("de");
        await effect.ExecuteAsync(CreateContext(catalogDialog.Object), dispatcher.Object, CancellationToken.None);

        var cancelledEvent = Assert.IsType<SpeechAssistantApplicationSelectionCancelledEvent>(dispatchedEvent);
        Assert.Equal("en", cancelledEvent.NormalizedEffectiveIsoCode);
        catalogDialog.VerifyAll();
        dispatcher.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenDialogThrows_PublishesFailureAndCancelsSelection()
    {
        var catalogDialog = new Mock<IFormsCatalogDialog>(MockBehavior.Strict);
        catalogDialog
            .Setup(dialog => dialog.ExecuteAsync("de", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus
            .Setup(bus => bus.PublishAsync(It.Is<SpeechCaptureFailed>(message =>
                message.ErrorCode == SpeechErrorCode.ApplicationCatalogDialogFailed
                && message.Message == "Application catalog dialog failed.")))
            .Returns(ValueTask.CompletedTask);

        SpeechAssistantEvent? dispatchedEvent = null;
        var dispatcher = CreateDispatcher(@event => dispatchedEvent = @event);

        var effect = new RunFormsCatalogDialogEffect("de");
        await effect.ExecuteAsync(CreateContext(catalogDialog.Object, messageBus.Object), dispatcher.Object, CancellationToken.None);

        Assert.IsType<SpeechAssistantApplicationSelectionCancelledEvent>(dispatchedEvent);
        catalogDialog.VerifyAll();
        messageBus.VerifyAll();
        dispatcher.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenIsoCodeIsInvalid_PublishesFailureAndCancelsSelection()
    {
        var catalogDialog = new Mock<IFormsCatalogDialog>(MockBehavior.Strict);
        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus
            .Setup(bus => bus.PublishAsync(It.Is<SpeechCaptureFailed>(message =>
                message.ErrorCode == SpeechErrorCode.ApplicationCatalogDialogFailed
                && message.Message == "Application catalog dialog failed.")))
            .Returns(ValueTask.CompletedTask);

        SpeechAssistantEvent? dispatchedEvent = null;
        var dispatcher = CreateDispatcher(@event => dispatchedEvent = @event);

        var effect = new RunFormsCatalogDialogEffect("german");
        await effect.ExecuteAsync(CreateContext(catalogDialog.Object, messageBus.Object), dispatcher.Object, CancellationToken.None);

        Assert.IsType<SpeechAssistantApplicationSelectionCancelledEvent>(dispatchedEvent);
        catalogDialog.VerifyNoOtherCalls();
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
        IFormsCatalogDialog catalogDialog,
        IClientMessageBus? messageBus = null)
    {
        ArgumentNullException.ThrowIfNull(catalogDialog);

        var speechClient = new NoOpSpeechInputClient();
        return SpeechAssistantContextTestFactory.Create(
            messageBus is null ? new NoOpClientMessageBus() : SpeechInteractionSignalsTestFactory.Create(messageBus),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            speechClient,
            null,
            speechClient,
            new NoOpTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            new FixedWakeWordProvider(["Okay"]),
            catalogDialog,
            new NoOpFormSelectionDialog(),
            new NoOpVoiceCommandDialog(),
            NullLogger<SpeechAssistantService>.Instance);
    }
}
