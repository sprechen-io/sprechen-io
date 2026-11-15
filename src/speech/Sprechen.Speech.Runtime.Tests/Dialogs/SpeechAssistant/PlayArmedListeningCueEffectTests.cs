using Microsoft.Extensions.Logging;
using Moq;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class PlayArmedListeningCueEffectTests
{
    [Fact]
    public async Task ExecuteAsync_WhenInvoked_PlaysFlowListeningCue()
    {
        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer
            .Setup(player => player.PlayAsync(EarconId.FlowListening, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        var context = CreateContext(earconPlayer.Object);
        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);

        var effect = new PlayArmedListeningCueEffect();
        await effect.ExecuteAsync(context, dispatcher.Object, CancellationToken.None);

        earconPlayer.Verify(player => player.PlayAsync(EarconId.FlowListening, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEarconPlaybackFails_DoesNotThrow()
    {
        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Strict);
        earconPlayer
            .Setup(player => player.PlayAsync(EarconId.FlowListening, CancellationToken.None))
            .Returns(() => ValueTask.FromException(new InvalidOperationException("boom")));

        var context = CreateContext(earconPlayer.Object);
        var dispatcher = new Mock<IEventDispatcher<SpeechAssistantEvent>>(MockBehavior.Strict);

        var effect = new PlayArmedListeningCueEffect();
        await effect.ExecuteAsync(context, dispatcher.Object, CancellationToken.None);

        earconPlayer.Verify(player => player.PlayAsync(EarconId.FlowListening, CancellationToken.None), Times.Once);
    }

    private static SpeechAssistantContext CreateContext(IEarconPlayer earconPlayer)
    {
        ArgumentNullException.ThrowIfNull(earconPlayer);

        return SpeechAssistantContextTestFactory.Create(
            new NoOpClientMessageBus(),
            new UserLanguageState(),
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechRecognitionClient>(),
            Mock.Of<ITextToSpeechPlayer>(),
            earconPlayer,
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            Mock.Of<ILogger<ISpeechAssistantService>>());
    }
}
