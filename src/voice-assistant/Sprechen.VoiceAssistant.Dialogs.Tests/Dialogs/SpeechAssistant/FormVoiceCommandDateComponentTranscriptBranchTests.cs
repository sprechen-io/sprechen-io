using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Weitergabe der Konfidenz vom schrittweise erfassten Datum.
/// Ein nicht als vollständiges Datum verwendeter Anfangstext darf die
/// Konfidenz der später erfassten Bestandteile nicht ersetzen. Wird bereits
/// der Anfangstext als vollständiges Datum übernommen, zählt dessen Konfidenz.
/// </summary>
public sealed class FormVoiceCommandDateComponentTranscriptBranchTests
{
    [Theory]
    [InlineData("murmur", 95)]
    [InlineData("murmur", 30)]
    [InlineData("8", 95)]      // Auch eine einzelne Ziffer wird nicht als erster Datumsbestandteil übernommen.
    public async Task NonEscapeInitialTurn_WithConfidence_CarriesUnknownConfidenceToTheApplyGate(
        string initialTranscript,
        int initialConfidence)
    {
        var result = await RunBranchAsync(
            initialTranscript,
            initialConfidence,
            followUpCaptures: new Queue<(string? Transcript, int? Confidence)>([("8", null), ("1", null), ("1980", null)]));

        Assert.NotNull(result);
        Assert.False(result!.HasFinalResult);
        Assert.Equal("1980-01-08", result.RequiredTranscriptForApply);
        Assert.Null(result.SttConfidencePercent);
    }

    [Fact]
    public async Task EscapeInitialTurn_ContributesItsOwnConfidence()
    {
        var result = await RunBranchAsync(
            "achter januar neunzehnhundertachtzig",
            initialConfidence: 62,
            followUpCaptures: new Queue<(string? Transcript, int? Confidence)>());

        Assert.NotNull(result);
        Assert.False(result!.HasFinalResult);
        Assert.Equal("1980-01-08", result.RequiredTranscriptForApply);
        Assert.Equal(62, result.SttConfidencePercent);
    }

    private static async Task<FormVoiceCommandFieldOrchestrationResult?> RunBranchAsync(
        string initialTranscript,
        int? initialConfidence,
        Queue<(string? Transcript, int? Confidence)> followUpCaptures)
    {
        var parser = new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());

        var speech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
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

        var driver = new DateComponentEntryWorkflowDriver(speech.Object, parser, parser);
        var branch = new FormVoiceCommandDateComponentTranscriptBranch(driver);

        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose).Object;
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object;
        var turnContext = new FormVoiceCommandFieldTurnContext(
            SpellingWorkflowTestFactory.CreateContext("de"),
            interpreter,
            CreateDateField(),
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

    private static FormFieldSnapshot CreateDateField()
        => new(
            Identity: new FormFieldIdentitySnapshot(FieldId: "birthdate", VariableId: "birthdate", FieldGroupId: "main"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Geburtsdatum",
                HelpText: null,
                Examples: [],
                Options: []),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Digits,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.Date,
                SpeakValueInConfirmation: true));
    private static FormVoiceCommandStateTracker ArmInterpretedFieldLoop(FormVoiceCommandStateTracker tracker)
    {
        tracker.BeginFieldPrompt("main");
        tracker.BeginListening();
        tracker.BeginInterpretation();
        return tracker;
    }

}
