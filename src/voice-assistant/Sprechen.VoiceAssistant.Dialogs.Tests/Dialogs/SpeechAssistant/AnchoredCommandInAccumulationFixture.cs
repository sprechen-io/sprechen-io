using System.Collections.Immutable;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.TestDoubles;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Testaufbau für Befehle während einer zusammengesetzten Eingabe. Produktive Ablaufdienste
/// arbeiten mit einem Parser-Mock, der festgelegte Wörter in Buchstaben oder Ziffern übersetzt.
/// Ein weiterer Mock prüft die erwarteten Aufrufe der Befehlsweiterleitung und kann bei einem Sprung das aktuelle Feld wechseln.
/// </summary>
internal sealed class AnchoredCommandInAccumulationFixture
{
    private static readonly ImmutableArray<string> WakeWords = ["okay alex"];

    private static readonly (string? Transcript, int? Confidence) Silence = (null, null);

    private static readonly IReadOnlyDictionary<string, string> LetterMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Anton"] = "a",
        ["Berta"] = "b",
        ["Cesar"] = "c"
    };

    private static readonly ImmutableArray<FormChoiceOption> ContactOptions =
    [
        new FormChoiceOption("email", "E-Mail"),
        new FormChoiceOption("sms", "SMS")
    ];

    private readonly Queue<(string? Transcript, int? Confidence)> _captures = new();
    private readonly Mock<IFormVoiceCommandSpeechService> _speech = new(MockBehavior.Loose);
    private readonly Mock<IFormDialogRuntime> _interpreter = new(MockBehavior.Loose);
    private FormFieldSnapshot _focusedField;

    public AnchoredCommandInAccumulationFixture()
    {
        Parser = new Mock<IFormTranscriptParser>(MockBehavior.Loose);
        Parser
            .Setup(p => p.ParseSpelling(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((_, transcript) =>
            {
                var letters = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(word => LetterMap.TryGetValue(word, out var letter) ? letter : null)
                    .ToArray();
                return letters.Length > 0 && letters.All(l => l is not null)
                    ? FormTranscriptParseOutcome.Succeeded(string.Concat(letters))
                    : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
            });
        Parser
            .Setup(p => p.ParseDigits(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((_, transcript) =>
            {
                var digits = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(word => word.ToLowerInvariant() switch { "eins" => "1", "zwei" => "2", "drei" => "3", _ => null })
                    .ToArray();
                return digits.Length > 0 && digits.All(d => d is not null)
                    ? FormTranscriptParseOutcome.Succeeded(string.Concat(digits))
                    : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
            });
        Parser
            .Setup(p => p.ParseChoice(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<ImmutableArray<FormChoiceOption>>(), It.IsAny<bool>()))
            .Returns<string?, string, ImmutableArray<FormChoiceOption>, bool>((_, transcript, _, _) =>
                ContactOptions.Any(option => string.Equals(option.Code, transcript.Trim(), StringComparison.OrdinalIgnoreCase))
                    ? FormTranscriptParseOutcome.Succeeded(transcript.Trim().ToLowerInvariant())
                    : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized));

        _speech
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, VoiceDialogSettings, CancellationToken>((text, _, _, _) =>
            {
                Spoken.Add(text);
                return Task.CompletedTask;
            });
        _speech
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
                    var (transcript, confidence) = _captures.Dequeue();
                    return transcript is null
                        ? Task.FromException<SpeechCaptureResult>(new NoSpeechDetectedException())
                        : Task.FromResult(new SpeechCaptureResult(transcript, confidence));
                });
        _speech.ForwardSingleAttemptCaptureToValueCapture();

        Router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        _focusedField = CreateField(InputMode.Spelling);
        _interpreter.Setup(i => i.GetFocusedFieldAsync()).ReturnsAsync(() => _focusedField);
    }

    public Mock<IFormTranscriptParser> Parser { get; }

    public Mock<IFormVoiceCommandGlobalCommandRouter> Router { get; }

    public List<string> Spoken { get; } = [];

    public int RemainingCaptures => _captures.Count;

    public void RouterReturnsNull(string commandId)
        => Router
            .Setup(r => r.TryHandleAsync(It.Is<CommandId>(c => c.Value == commandId), It.IsAny<FormVoiceCommandGlobalCommandContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoiceCommandDialogResult?)null);

    // Der Handler wechselt den Fokus und liefert danach null, wie der echte Sprung-Handler.
    public void RouterMovesFocusTo(string commandId, string expectedArgument, FormFieldSnapshot target)
        => Router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == commandId),
                It.Is<FormVoiceCommandGlobalCommandContext>(c => string.Equals(c.Request.Argument, expectedArgument, StringComparison.OrdinalIgnoreCase)),
                It.IsAny<CancellationToken>()))
            .Returns<CommandId, FormVoiceCommandGlobalCommandContext, CancellationToken>((_, _, _) =>
            {
                _focusedField = target;
                return Task.FromResult<VoiceCommandDialogResult?>(null);
            });

    public FormFieldSnapshot CreateField(InputMode inputMode, string fieldId = "name")
        => new(
            Identity: new FormFieldIdentitySnapshot(FieldId: fieldId, VariableId: fieldId, FieldGroupId: "main"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Name",
                HelpText: null,
                Examples: [],
                Options: ContactOptions),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: inputMode,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.String,
                SpeakValueInConfirmation: true));

    public async Task<CharacterAccumulationResult> RunSpellingAsync(string initialTranscript, params (string? Transcript, int? Confidence)[] captures)
    {
        Enqueue(captures);
        var evaluationTelemetry = new Mock<IEvaluationFormTelemetry>(MockBehavior.Loose).Object;
        var renderer = new Mock<IPhoneticSpellOutRenderer>(MockBehavior.Loose);
        renderer.Setup(r => r.Render(It.IsAny<string>(), It.IsAny<string>())).Returns<string, string>((value, _) => $"PHON[{value}]");
        var privacy = new Mock<IFormDialogPrivacyReadbackPolicy>(MockBehavior.Loose);
        privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync(It.IsAny<string>())).ReturnsAsync(true);

        var service = new FormVoiceCommandSpellingWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            CreateAnchoredFlow(),
            new SpellingWorkflowCommandResolver(new SpellingWorkflowLocalCommandMapper(), new SpellingWorkflowGlobalCommandMapper()),
            Parser.Object,
            _speech.Object,
            new SpellingWorkflowCommandActionFlow(evaluationTelemetry, _speech.Object, new SpellingWorkflowNoValuePrompt(_speech.Object)),
            new SpellingWorkflowInputRejectionFlow(new WorkflowUnrecognizedInputAnnouncer(evaluationTelemetry, _speech.Object)),
            renderer.Object);

        var (session, stateTracker, autoHelp) = CreateTurnParts();
        _focusedField = CreateField(InputMode.Spelling);
        return await service.CaptureSpellingTranscriptAsync(
            SpellingWorkflowTestFactory.CreateContext("de", WakeWords),
            _interpreter.Object,
            _focusedField,
            session,
            stateTracker,
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            privacy.Object,
            initialTranscript,
            90,
            CancellationToken.None);
    }

    public async Task<CharacterAccumulationResult> RunDigitsAsync(string initialTranscript, params (string? Transcript, int? Confidence)[] captures)
    {
        Enqueue(captures);
        var evaluationTelemetry = new Mock<IEvaluationFormTelemetry>(MockBehavior.Loose).Object;
        var service = new FormVoiceCommandDigitsWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            CreateAnchoredFlow(),
            Parser.Object,
            new DigitsWorkflowCommandResolver(new DigitsWorkflowLocalCommandMapper(), new DigitsWorkflowGlobalCommandMapper()),
            new DigitsWorkflowCommandActionFlow(evaluationTelemetry, _speech.Object, new DigitsWorkflowNoValuePrompt(_speech.Object)),
            new DigitsWorkflowInputRejectionFlow(new WorkflowUnrecognizedInputAnnouncer(evaluationTelemetry, _speech.Object)),
            new DigitsWorkflowTranscriptCapture(_speech.Object));

        var (session, stateTracker, autoHelp) = CreateTurnParts();
        _focusedField = CreateField(InputMode.Digits);
        return await service.CaptureDigitsTranscriptAsync(
            SpellingWorkflowTestFactory.CreateContext("de", WakeWords),
            _interpreter.Object,
            _focusedField,
            session,
            stateTracker,
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            initialTranscript,
            90,
            CancellationToken.None);
    }

    public async Task<CharacterAccumulationResult> RunSetAsync(string initialTranscript, params (string? Transcript, int? Confidence)[] captures)
    {
        Enqueue(captures);
        var evaluationTelemetry = new Mock<IEvaluationFormTelemetry>(MockBehavior.Loose).Object;
        var service = new FormVoiceCommandSetWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            CreateAnchoredFlow(),
            new SetWorkflowCommandResolver(new SetWorkflowGlobalCommandMapper()),
            new SetWorkflowCommandActionFlow(evaluationTelemetry),
            new SetWorkflowInputRejectionFlow(new WorkflowUnrecognizedInputAnnouncer(evaluationTelemetry, _speech.Object)),
            Parser.Object,
            _speech.Object);

        var (session, stateTracker, autoHelp) = CreateTurnParts();
        _focusedField = CreateField(InputMode.Voice);
        return await service.CaptureSetTranscriptAsync(
            SpellingWorkflowTestFactory.CreateContext("de", WakeWords),
            _interpreter.Object,
            _focusedField,
            session,
            stateTracker,
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            initialTranscript,
            90,
            CancellationToken.None);
    }

    private CharacterAccumulationAnchoredCommandFlow CreateAnchoredFlow()
        => AnchoredCommandFlowTestFactory.Create(
            new FormVoiceCommandGlobalCommandExecutionService(
                new Mock<IEvaluationCommandTelemetry>(MockBehavior.Loose).Object,
                Router.Object),
            _speech.Object);

    private void Enqueue((string? Transcript, int? Confidence)[] captures)
    {
        _captures.Clear();
        foreach (var capture in captures)
        {
            _captures.Enqueue(capture);
        }

        // Zwei simulierte Eingaben ohne Sprache beenden den Ablauf auch ohne Abschlusswort.
        _captures.Enqueue(Silence);
        _captures.Enqueue(Silence);
    }

    private static (EvaluationSession Session, FormVoiceCommandStateTracker StateTracker, FormVoiceCommandAutoHelpTracker AutoHelp) CreateTurnParts()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose).Object;
        var stateTracker = new FormVoiceCommandStateTracker(telemetry, session);
        stateTracker.BeginFieldPrompt("main");
        stateTracker.BeginListening();
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            new VoiceDialogSettings(),
            new Mock<IFormVoiceCommandPromptService>(MockBehavior.Loose).Object,
            new Mock<IFormVoiceCommandDialogStateTracker>(MockBehavior.Loose).Object,
            new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object,
            "de");
        return (session, stateTracker, autoHelp);
    }
}
