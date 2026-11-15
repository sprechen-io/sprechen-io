using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Immutable;
using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.TestDoubles;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Übergabe einer leeren optionalen Mehrfachauswahl und eines ausgewählten Codes an die nachfolgende Wertverarbeitung.
/// Die Konfidenz stammt nur aus Beiträgen zur fertigen Auswahl.
/// </summary>
public sealed class FormVoiceCommandSetTranscriptBranchTests
{
    private static readonly ImmutableArray<FormChoiceOption> ContactOptions =
    [
        new FormChoiceOption("email", "E-Mail"),
        new FormChoiceOption("phone", "Telefon"),
    ];

    [Fact]
    public async Task OptionalSet_DoneOnEmpty_ContinuesIntoApplyWithTheCanonicalEmptySet()
    {
        // Bei Mindestanzahl null darf „fertig“ ohne Auswahl eine kanonische leere Menge zurückgeben.
        var result = await RunBranchAsync(
            minSelected: 0,
            initialTranscript: "fertig",
            followUpCaptures: new Queue<(string? Transcript, int? Confidence)>());

        Assert.NotNull(result);
        Assert.False(result!.HasFinalResult);
        Assert.Equal(string.Empty, result.RequiredTranscriptForApply);
    }

    [Fact]
    public async Task RequiredSet_DoneOnEmpty_RefusesAndOnlyContinuesAfterASelection()
    {
        // Bei Mindestanzahl eins endet das Sammeln erst nach Auswahl einer Option.
        var result = await RunBranchAsync(
            minSelected: 1,
            initialTranscript: "fertig",
            followUpCaptures: new Queue<(string? Transcript, int? Confidence)>([("email", 90), ("fertig", 95)]));

        Assert.NotNull(result);
        Assert.False(result!.HasFinalResult);
        Assert.Equal("email", result.RequiredTranscriptForApply);
    }

    // Ein verworfener Beitrag oder reiner Befehl darf keine Konfidenz für den späteren Feldwert liefern.
    // Fehlt den eigentlichen Auswahlbeiträgen die Konfidenz, bleibt das Gesamtergebnis unbekannt.
    [Theory]
    [InlineData("murmur")]      // Verworfener erster Beitrag ohne erkannte Option.
    [InlineData("wiederholen")] // Erster Beitrag ist nur ein Wiederholungsbefehl.
    public async Task NonContributingInitialTurn_WithConfidence_CarriesUnknownConfidenceToTheApplyGate(string initialTranscript)
    {
        var result = await RunBranchAsync(
            minSelected: 1,
            initialTranscript: initialTranscript,
            followUpCaptures: new Queue<(string? Transcript, int? Confidence)>([("email", null), ("fertig", null)]));

        Assert.NotNull(result);
        Assert.False(result!.HasFinalResult);
        Assert.Equal("email", result.RequiredTranscriptForApply);
        Assert.Null(result.SttConfidencePercent);
    }

    private static async Task<FormVoiceCommandFieldOrchestrationResult?> RunBranchAsync(
        int? minSelected,
        string initialTranscript,
        Queue<(string? Transcript, int? Confidence)> followUpCaptures)
    {
        var formTelemetry = new Mock<IEvaluationFormTelemetry>(MockBehavior.Loose);

        var parser = new Mock<IFormTranscriptParser>(MockBehavior.Loose);
        parser
            .Setup(p => p.ParseChoice(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<ImmutableArray<FormChoiceOption>>(), true))
            .Returns<string?, string, ImmutableArray<FormChoiceOption>, bool>((_, transcript, _, _) =>
                ContactOptions.Any(option => string.Equals(option.Code, transcript.Trim(), StringComparison.OrdinalIgnoreCase))
                    ? FormTranscriptParseOutcome.Succeeded(transcript.Trim().ToLowerInvariant())
                    : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized));

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
        speech.ForwardSingleAttemptCaptureToValueCapture();
        // Die Einzelaufnahme wird an die vorbereitete Antwortfolge des Mocks weitergeleitet.

        var branch = new FormVoiceCommandSetTranscriptBranch(
            new FormVoiceCommandSetWorkflowService(
                new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
                AnchoredCommandFlowTestFactory.Create(),
                new SetWorkflowCommandResolver(new SetWorkflowGlobalCommandMapper()),
                new SetWorkflowCommandActionFlow(formTelemetry.Object),
                new SetWorkflowInputRejectionFlow(
                    new WorkflowUnrecognizedInputAnnouncer(formTelemetry.Object, speech.Object)),
                parser.Object,
                speech.Object));

        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose).Object;
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object;
        var turnContext = new FormVoiceCommandFieldTurnContext(
            SpellingWorkflowTestFactory.CreateContext("de"),
            interpreter,
            CreateSetField(minSelected),
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
                IsDigitsMode: false,
                ShouldExecuteParsedCommand: false,
                ShouldExecuteParsedCommandInDigitsMode: false));

        return await branch.TryHandleAsync(
            turnContext,
            interpretation,
            sttConfidencePercent: 95,
            CancellationToken.None);
    }

    private static FormFieldSnapshot CreateSetField(int? minSelected)
        => new(
            Identity: new FormFieldIdentitySnapshot(FieldId: "contactChannels", VariableId: "contactChannels", FieldGroupId: "main"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Kontaktkanaele",
                HelpText: null,
                Examples: [],
                Options: ContactOptions),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.Set,
                SpeakValueInConfirmation: true)
            {
                MinSelected = minSelected,
                MaxSelected = 3
            });
    private static FormVoiceCommandStateTracker ArmInterpretedFieldLoop(FormVoiceCommandStateTracker tracker)
    {
        tracker.BeginFieldPrompt("main");
        tracker.BeginListening();
        tracker.BeginInterpretation();
        return tracker;
    }

}
