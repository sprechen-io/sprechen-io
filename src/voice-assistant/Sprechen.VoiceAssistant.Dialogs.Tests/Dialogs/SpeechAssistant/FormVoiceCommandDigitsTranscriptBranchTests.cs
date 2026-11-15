using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.TestDoubles;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, dass nur zum Ziffernwert beitragende Eingaben dessen Konfidenz
/// bestimmen. Fehlen dort Konfidenzen, bleibt das Ergebnis unbekannt;
/// ein verworfener Anfangstext oder reiner Befehl darf es nicht ersetzen.
/// Der Ziffernparser ist für diese Prüfung simuliert.
/// </summary>
public sealed class FormVoiceCommandDigitsTranscriptBranchTests
{
    [Theory]
    [InlineData("Quatsch", 95)]
    [InlineData("Quatsch", 30)]
    [InlineData("letzten block löschen", 95)] // Reiner Löschbefehl bei zunächst leerem Puffer.
    public async Task NonContributingInitialTurn_WithConfidence_CarriesUnknownConfidenceToTheApplyGate(
        string initialTranscript,
        int initialConfidence)
    {
        var result = await RunBranchAsync(
            initialTranscript,
            initialConfidence,
            followUpCaptures: new Queue<(string? Transcript, int? Confidence)>([("eins zwei drei", null), (null, null), (null, null)]));

        Assert.NotNull(result);
        Assert.False(result!.HasFinalResult);
        Assert.Equal("123", result.RequiredTranscriptForApply);
        Assert.Null(result.SttConfidencePercent);
    }

    private static async Task<FormVoiceCommandFieldOrchestrationResult?> RunBranchAsync(
        string initialTranscript,
        int? initialConfidence,
        Queue<(string? Transcript, int? Confidence)> followUpCaptures)
    {
        var formTelemetry = new Mock<IEvaluationFormTelemetry>(MockBehavior.Loose);

        var parser = new Mock<IFormTranscriptParser>(MockBehavior.Loose);
        parser
            .Setup(p => p.ParseDigits(It.IsAny<string?>(), It.IsAny<string>()))
            .Returns((string? _, string transcript) =>
                string.Equals(transcript.Trim(), "eins zwei drei", StringComparison.OrdinalIgnoreCase)
                    ? FormTranscriptParseOutcome.Succeeded("123")
                    : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized));

        var speech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speech
            .Setup(s => s.SpeakSafeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        speech
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>(
                (_, _, _, onStartListening, _, _, _, _) =>
                {
                    onStartListening();
                    var (transcript, confidence) = followUpCaptures.Dequeue();
                    return transcript is null
                        ? Task.FromException<SpeechCaptureResult>(new NoSpeechDetectedException())
                        : Task.FromResult(new SpeechCaptureResult(transcript, confidence));
                });
        speech.ForwardSingleAttemptCaptureToValueCapture();

        var service = new FormVoiceCommandDigitsWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(),
            parser.Object,
            new DigitsWorkflowCommandResolver(
                new DigitsWorkflowLocalCommandMapper(),
                new DigitsWorkflowGlobalCommandMapper()),
            new DigitsWorkflowCommandActionFlow(
                formTelemetry.Object,
                speech.Object,
                new DigitsWorkflowNoValuePrompt(speech.Object)),
            new DigitsWorkflowInputRejectionFlow(
                new WorkflowUnrecognizedInputAnnouncer(formTelemetry.Object, speech.Object)),
            new DigitsWorkflowTranscriptCapture(speech.Object));
        var branch = new FormVoiceCommandDigitsTranscriptBranch(service);

        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose).Object;
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object;
        var turnContext = new FormVoiceCommandFieldTurnContext(
            SpellingWorkflowTestFactory.CreateContext("de"),
            interpreter,
            SpellingWorkflowTestFactory.CreateField(),
            session,
            ArmInterpretedFieldLoop(new FormVoiceCommandStateTracker(telemetry, session)),
            new FormVoiceCommandAutoHelpTracker(
                new VoiceDialogSettings(),
                new Mock<IFormVoiceCommandPromptService>(MockBehavior.Loose).Object,
                new Mock<IFormVoiceCommandDialogStateTracker>(MockBehavior.Loose).Object,
                interpreter,
                "de"),
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            new Mock<IFormDialogPrivacyReadbackPolicy>(MockBehavior.Loose).Object);

        var interpretation = new FormVoiceCommandTranscriptInterpretation(
            new FormVoiceCommandTranscriptSource(
                TrimmedTranscript: initialTranscript,
                InputTranscript: initialTranscript,
                HasExplicitCommandPrefix: false),
            new FormVoiceCommandParsedCommand(
                HasParsedCommand: false,
                ParsedCommandId: string.Empty,
                ParsedCommandArgument: null,
                CommandSignalClass: string.Empty),
            new FormVoiceCommandInterpretationMode(
                IsTextField: false,
                IsLiteralMode: false,
                IsDigitsMode: true,
                ShouldExecuteParsedCommand: false,
                ShouldExecuteParsedCommandInDigitsMode: false));

        return await branch.TryHandleAsync(
            turnContext,
            interpretation,
            initialConfidence,
            CancellationToken.None);
    }

    private static FormVoiceCommandStateTracker ArmInterpretedFieldLoop(FormVoiceCommandStateTracker tracker)
    {
        tracker.BeginFieldPrompt("main");
        tracker.BeginListening();
        tracker.BeginInterpretation();
        return tracker;
    }

}
