using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Fehlermeldung, Reparaturcode und angeforderten Ausgabetext
/// bei nicht auswertbaren Buchstabier- und Zifferneingaben.
/// </summary>
public sealed class SpellingWorkflowInputRejectionFlowTests
{
    [Fact]
    public async Task HandleAsync_RecordsUnrecognizedRitual_WithSpellingRepairCode_AndSpeaks()
    {
        var spoken = new List<string>();
        var telemetry = new Mock<IEvaluationFormTelemetry>(MockBehavior.Loose);
        var flow = new SpellingWorkflowInputRejectionFlow(
            new WorkflowUnrecognizedInputAnnouncer(telemetry.Object, CreateSpeech(spoken)));

        await flow.HandleAsync(
            SpellingWorkflowTestFactory.CreateContext("de"),
            SpellingWorkflowTestFactory.CreateField(),
            CreateSession(),
            CreateStateTracker(),
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            "voll wirres zeug",
            CancellationToken.None);

        telemetry.Verify(
            t => t.TrackConstraintViolation(
                It.IsAny<EvaluationSession>(),
                "name",
                FormVoiceCommandFieldErrorCodes.UnrecognizedInput,
                SeverityLevels.Warning,
                TelemetryErrorTriggers.SystemRecognition),
            Times.Once);
        telemetry.Verify(
            t => t.TrackRepairAttempt(
                It.IsAny<EvaluationSession>(),
                "name",
                SpellingWorkflowRepairEventCodes.UnrecognizedCharacters),
            Times.Once);
        Assert.Single(spoken);
        Assert.NotEqual(string.Empty, spoken[0].Trim());
    }

    [Fact]
    public async Task SharedAnnouncer_RecordsTheCallerSuppliedRepairCode()
    {
        // Die gemeinsame Fehlerbehandlung muss den Reparaturcode des jeweiligen Eingabemodus verwenden.
        var telemetry = new Mock<IEvaluationFormTelemetry>(MockBehavior.Loose);
        var digitsFlow = new DigitsWorkflowInputRejectionFlow(
            new WorkflowUnrecognizedInputAnnouncer(telemetry.Object, CreateSpeech(new List<string>())));

        await digitsFlow.HandleAsync(
            SpellingWorkflowTestFactory.CreateContext("de"),
            SpellingWorkflowTestFactory.CreateField(),
            CreateSession(),
            CreateStateTracker(),
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            "nicht erkannt",
            CancellationToken.None);

        telemetry.Verify(
            t => t.TrackRepairAttempt(
                It.IsAny<EvaluationSession>(),
                "name",
                DigitsWorkflowRepairEventCodes.UnrecognizedBlock),
            Times.Once);
        telemetry.Verify(
            t => t.TrackRepairAttempt(
                It.IsAny<EvaluationSession>(),
                "name",
                SpellingWorkflowRepairEventCodes.UnrecognizedCharacters),
            Times.Never);
    }

    private static IFormVoiceCommandSpeechService CreateSpeech(List<string> spoken)
    {
        var speech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speech
            .Setup(s => s.SpeakSafeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, VoiceDialogSettings, CancellationToken>((text, _, _, _) =>
            {
                spoken.Add(text);
                return Task.CompletedTask;
            });
        return speech.Object;
    }

    private static FormVoiceCommandStateTracker CreateStateTracker()
    {
        var session = CreateSession();
        var tracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>(MockBehavior.Loose).Object, session);
        tracker.BeginFieldPrompt("main");
        tracker.BeginListening();
        return tracker;
    }

    private static EvaluationSession CreateSession()
        => new("test-session", EvaluationChannel.Voice, "test-app", "form-1");
}
