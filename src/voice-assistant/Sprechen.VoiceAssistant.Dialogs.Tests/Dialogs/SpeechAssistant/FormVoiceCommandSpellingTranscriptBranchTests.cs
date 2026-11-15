using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.TestDoubles;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Wert und Konfidenz, die der Buchstabierablauf an die folgende Wertverarbeitung übergibt.
/// Nur erhaltene Zeichenbeiträge dürfen zur Gesamtkonfidenz beitragen; fehlende Konfidenz bleibt unbekannt.
/// Der Namensfall verwendet den echten Parser, die übrigen Konfidenzfälle einen Mock.
/// </summary>
public sealed class FormVoiceCommandSpellingTranscriptBranchTests
{
    [Fact]
    public async Task StudyMartaSpelling_ReachesApplyAsSpokenWithoutAnExpectedAnswerTerm()
    {
        var parser = new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());

        var result = await RunBranchAsync(
            "capital mike alpha romeo tango alpha",
            followUpCaptures: new Queue<(string? Transcript, int? Confidence)>([(null, null), (null, null)]),
            parserOverride: parser,
            isoCode: "en");

        Assert.NotNull(result);
        Assert.False(result!.HasFinalResult);
        Assert.Equal("Marta", result.RequiredTranscriptForApply);
        Assert.Equal(95, result.SttConfidencePercent);
    }

    [Theory]
    [InlineData("Quatsch")]               // Verworfener erster Beitrag ohne erkannten Buchstaben.
    [InlineData("letzten Block löschen")] // Reiner Löschbefehl bei leerem Zeichenspeicher.
    public async Task NonContributingInitialTurn_WithConfidence_CarriesUnknownConfidenceToTheApplyGate(string initialTranscript)
    {
        var result = await RunBranchAsync(
            initialTranscript,
            followUpCaptures: new Queue<(string? Transcript, int? Confidence)>([("Anton", null), (null, null), (null, null)]));

        Assert.NotNull(result);
        Assert.False(result!.HasFinalResult);
        Assert.Equal("a", result.RequiredTranscriptForApply);
        Assert.Null(result.SttConfidencePercent);
    }

    private static async Task<FormVoiceCommandFieldOrchestrationResult?> RunBranchAsync(
        string initialTranscript,
        Queue<(string? Transcript, int? Confidence)> followUpCaptures,
        IFormTranscriptParser? parserOverride = null,
        string isoCode = "de")
    {
        var formTelemetry = new Mock<IEvaluationFormTelemetry>(MockBehavior.Loose);

        var transcriptParser = parserOverride;
        if (transcriptParser is null)
        {
            var parser = new Mock<IFormTranscriptParser>(MockBehavior.Loose);
            parser
                .Setup(p => p.ParseSpelling(It.IsAny<string?>(), It.IsAny<string>()))
                .Returns<string?, string>((_, transcript) =>
                    string.Equals(transcript.Trim(), "Anton", StringComparison.OrdinalIgnoreCase)
                        ? FormTranscriptParseOutcome.Succeeded("a")
                        : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized));
            transcriptParser = parser.Object;
        }

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

        var renderer = new Mock<IPhoneticSpellOutRenderer>(MockBehavior.Loose);
        renderer
            .Setup(r => r.Render(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, _) => $"PHON[{value}]");

        var service = new FormVoiceCommandSpellingWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(),
            new SpellingWorkflowCommandResolver(
                new SpellingWorkflowLocalCommandMapper(),
                new SpellingWorkflowGlobalCommandMapper()),
            transcriptParser,
            speech.Object,
            new SpellingWorkflowCommandActionFlow(
                formTelemetry.Object,
                speech.Object,
                new SpellingWorkflowNoValuePrompt(speech.Object)),
            new SpellingWorkflowInputRejectionFlow(
                new WorkflowUnrecognizedInputAnnouncer(formTelemetry.Object, speech.Object)),
            renderer.Object);
        var branch = new FormVoiceCommandSpellingTranscriptBranch(service);

        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose).Object;
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object;
        var privacy = new Mock<IFormDialogPrivacyReadbackPolicy>(MockBehavior.Loose);
        privacy
            .Setup(p => p.IsSpellOutAllowedInConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        var turnContext = new FormVoiceCommandFieldTurnContext(
            SpellingWorkflowTestFactory.CreateContext(isoCode),
            interpreter,
            SpellingWorkflowTestFactory.CreateField(),
            session,
            ArmInterpretedFieldLoop(new FormVoiceCommandStateTracker(telemetry, session)),
            new FormVoiceCommandAutoHelpTracker(
                new VoiceDialogSettings(),
                new Mock<IFormVoiceCommandPromptService>(MockBehavior.Loose).Object,
                new Mock<IFormVoiceCommandDialogStateTracker>(MockBehavior.Loose).Object,
                interpreter,
                isoCode),
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            privacy.Object);

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
                IsTextField: true,
                IsLiteralMode: false,
                IsDigitsMode: false,
                ShouldExecuteParsedCommand: false,
                ShouldExecuteParsedCommandInDigitsMode: false,
                IsSpellingMode: true));

        return await branch.TryHandleAsync(
            turnContext,
            interpretation,
            sttConfidencePercent: 95,
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
