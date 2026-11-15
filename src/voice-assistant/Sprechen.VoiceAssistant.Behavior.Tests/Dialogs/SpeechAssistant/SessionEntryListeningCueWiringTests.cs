using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, dass der Sitzungseinstieg den registrierten Aufnahmeindikator statt der reinen Oberflächensignale verwendet.
/// Der Test unterscheidet die beiden Abhängigkeiten per Mock.
/// </summary>
public sealed class SessionEntryListeningCueWiringTests
{
    [Fact]
    public async Task TheEntry_ArmsItsMicrophoneThroughTheIndicatorThatPlaysTheCue()
    {
        var signals = new Mock<ISpeechInteractionSignals>();
        var cuePlayingIndicator = new Mock<ISpeechCaptureRecordingIndicator>();

        var turn = SessionEntryLanguageOfferTurn.Create(
            CreateContext(signals.Object, cuePlayingIndicator.Object));

        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await turn.DecideLanguageAsync("de", deadline.Token);

        // Der für den Aufnahmeton vorgesehene Indikator muss beim Einstieg verwendet werden.
        cuePlayingIndicator.Verify(indicator => indicator.BeginRecordingAsync(), Times.AtLeastOnce);

        // Die reine Oberflächensignalisierung darf nicht direkt als Aufnahmeindikator verwendet werden.
        signals.Verify(indicator => indicator.BeginRecordingAsync(), Times.Never);
    }

    private static SpeechAssistantContext CreateContext(
        ISpeechInteractionSignals signals,
        ISpeechCaptureRecordingIndicator recordingIndicator)
    {
        var userLanguageState = new TestDoubles.UserLanguageState();
        var speechInputClient = new Mock<ISpeechInputClient>();

        return SpeechAssistantContextTestFactory.Create(
            signals,
            userLanguageState,
            speechInputClient.Object,
            Mock.Of<ITextToSpeechPlayer>(),
            Mock.Of<IEarconPlayer>(),
            new FixedWakeWordProvider(["Okay"]),
            Mock.Of<IFormsCatalogDialog>(),
            new FixedFormSelectionDialog("main-form"),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<ISpeechAssistantService>.Instance,
            formsCatalogDataClient: null,
            recordingIndicator: recordingIndicator);
    }
}
