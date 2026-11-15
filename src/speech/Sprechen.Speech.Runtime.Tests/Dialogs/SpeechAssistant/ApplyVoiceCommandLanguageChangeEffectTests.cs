using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class ApplyVoiceCommandLanguageChangeEffectTests
{
    [Fact]
    public async Task ExecuteAsync_WhenWorkflowSucceeds_DispatchesLanguageChangedEvent()
    {
        var workflow = new Mock<IUserLanguageSettingsWorkflow>(MockBehavior.Strict);
        workflow
            .Setup(owner => owner.SaveSelectedLanguageAsync("en", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new UserLanguageSettings("en", PolitenessLevel.Formal)));

        var context = CreateContext(workflow.Object);
        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(
                It.Is<SpeechAssistantVoiceCommandLanguageChangedEvent>(evt => evt.NormalizedIsoCode == "en"),
                DispatchMode.Enqueue,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        await new ApplyVoiceCommandLanguageChangeEffect("en")
            .ExecuteAsync(context, dispatcher.Object, CancellationToken.None);

        workflow.VerifyAll();
        dispatcher.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenWorkflowThrows_PublishesFailureAndDispatchesFailureEvent()
    {
        var workflow = new Mock<IUserLanguageSettingsWorkflow>(MockBehavior.Strict);
        workflow
            .Setup(owner => owner.SaveSelectedLanguageAsync("en", It.IsAny<CancellationToken>()))
            .Returns((string? _, CancellationToken _) =>
                ValueTask.FromException<UserLanguageSettings>(new InvalidOperationException("boom")));

        var messageBus = new Mock<IClientMessageBus>(MockBehavior.Strict);
        messageBus
            .Setup(bus => bus.PublishAsync(It.Is<SpeechCaptureFailed>(message =>
                message.ErrorCode == SpeechErrorCode.VoiceCommandLanguageChangeFailed
                && message.Message == "Applying in-session language change failed.")))
            .Returns(ValueTask.CompletedTask);

        var context = CreateContext(workflow.Object, messageBus.Object);
        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);
        dispatcher
            .Setup(d => d.DispatchAsync(
                It.IsAny<SpeechAssistantVoiceCommandLanguageChangeFailedEvent>(),
                DispatchMode.Enqueue,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        await new ApplyVoiceCommandLanguageChangeEffect("en")
            .ExecuteAsync(context, dispatcher.Object, CancellationToken.None);

        workflow.VerifyAll();
        messageBus.VerifyAll();
        dispatcher.VerifyAll();
    }

    private static SpeechAssistantContext CreateContext(
        IUserLanguageSettingsWorkflow workflow,
        IClientMessageBus? messageBus = null)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        return SpeechAssistantContextTestFactory.Create(
            messageBus is null ? new NoOpClientMessageBus() : SpeechInteractionSignalsTestFactory.Create(messageBus),
            new UserLanguageState(),
            workflow,
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechInputClient>(),
            Mock.Of<ITextToSpeechPlayer>(),
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance);
    }
}
