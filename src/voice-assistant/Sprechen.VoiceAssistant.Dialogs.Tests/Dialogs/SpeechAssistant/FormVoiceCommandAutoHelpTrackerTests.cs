using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandAutoHelpTrackerTests
{
    [Fact]
    public async Task RegisterFailureAsync_WhenEscalationFires_ForwardsTheSessionLatchToThePromptService()
    {
        var voiceSettings = new VoiceDialogSettings { MaxRetries = 1 };
        var interpreter = Mock.Of<IFormDialogRuntime>();
        var stateTracker = new TestFormVoiceCommandDialogStateTracker();
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        promptService
            .Setup(item => item.SpeakHelpForFocusedFieldAsync(
                interpreter,
                stateTracker,
                "de",
                voiceSettings,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            voiceSettings,
            promptService.Object,
            stateTracker,
            interpreter,
            isoCode: "de");

        await autoHelp.RegisterFailureAsync("field", CancellationToken.None);

        // Die automatische Hilfe soll dieselbe Zustandsinstanz weitergeben,
        // damit bereits erteilte Hinweise in der Sitzung berücksichtigt werden.
        promptService.Verify(
            item => item.SpeakHelpForFocusedFieldAsync(
                interpreter,
                stateTracker,
                "de",
                voiceSettings,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
