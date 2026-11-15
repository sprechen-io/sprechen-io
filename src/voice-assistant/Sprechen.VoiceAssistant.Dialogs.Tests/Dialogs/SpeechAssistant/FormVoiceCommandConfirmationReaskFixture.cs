using System.Collections.Immutable;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt den Bestätigungsablauf mit echten Handlern und vorgegebenen Antworten
/// bereit. Simuliert Textantwort, fehlende Eingabe oder Aufnahmefehler und
/// sammelt die angeforderten Ansage- und Aufnahmetexte.
/// </summary>
internal sealed class FormVoiceCommandConfirmationReaskFixture
{
    public const string Silence = " silence";

    public const string TransientFault = " fault";

    private readonly Queue<string> _answers;
    private readonly Mock<IFormVoiceCommandSpeechService> _speech = new(MockBehavior.Loose);
    private readonly Mock<IFormDialogRuntime> _interpreter = new(MockBehavior.Loose);
    private readonly EvaluationSession _session = new("test-session", EvaluationChannel.Voice, "test-app", "form-1");
    private FormVoiceCommandStateTracker? _stateTracker;

    public FormVoiceCommandConfirmationReaskFixture(params string[] answers)
    {
        _answers = new Queue<string>(answers);
        _speech
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, VoiceDialogSettings, CancellationToken>((text, _, _, _) =>
            {
                Spoken.Add(text);
                return Task.CompletedTask;
            });
        _speech
            .Setup(s => s.CaptureConfirmationTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, SpeechKeyterms?, CancellationToken>(
                (prompt, _, _, onStartListening, _, _, _) =>
                {
                    CapturePrompts.Add(prompt);
                    onStartListening();
                    var answer = _answers.Dequeue();
                    return answer switch
                    {
                        Silence => Task.FromException<SpeechCaptureResult>(new NoSpeechDetectedException()),
                        TransientFault => Task.FromException<SpeechCaptureResult>(new SpeechCaptureStreamStalledException()),
                        _ => Task.FromResult(new SpeechCaptureResult(answer, 95))
                    };
                });
        _interpreter
            .Setup(i => i.GetValuesSnapshotAsync())
            .ReturnsAsync(ImmutableDictionary.CreateRange(StringComparer.OrdinalIgnoreCase, [new KeyValuePair<string, string>("name", "max")]));
    }

    public Mock<IEvaluationTelemetry> Telemetry { get; } = new(MockBehavior.Loose);

    public Mock<IFormVoiceCommandGlobalCommandRouter> Router { get; } = new(MockBehavior.Strict);

    public List<string> Spoken { get; } = [];

    public List<string> CapturePrompts { get; } = [];

    public bool FromSpellingOrDigits { get; init; }

    public bool AutoAdvanceAfterCommit { get; init; }

    public Mock<IFormDialogRuntime> Interpreter => _interpreter;

    public int ConfirmationTurnCount => _stateTracker!.ReparableConfirmationTurnCount;

    public void RouterReturnsNull(string commandId)
        => Router
            .Setup(r => r.TryHandleAsync(It.Is<CommandId>(c => c.Value == commandId), It.IsAny<FormVoiceCommandGlobalCommandContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoiceCommandDialogResult?)null);

    public async Task<FormVoiceCommandFieldHandlingResult> HandleAsync()
    {
        var executionService = new FormVoiceCommandGlobalCommandExecutionService(Telemetry.Object, Router.Object);
        var presenter = new FormVoiceCommandConfirmationValuePresenter(
            _speech.Object,
            new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var decisionService = new FormVoiceCommandConfirmationDecisionService(
            new FormVoiceCommandConfirmationTranscriptCapture(_speech.Object),
            new FormVoiceCommandConfirmationTranscriptInterpreter(
                new FormVoiceCommandConfirmationCommandDecisionMapper(),
                new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver())),
            new FormVoiceCommandConfirmationMissingDecisionPolicy());
        var handler = new FormVoiceCommandConfirmationHandler(
            presenter,
            decisionService,
            new FormVoiceCommandConfirmationRepairActionFlow(Telemetry.Object, _speech.Object, executionService),
            new FormVoiceCommandConfirmedFieldCompletionFlow(
                _speech.Object,
                Mock.Of<IEarconPlayer>(),
                new FormClosingReviewTurnFlow(_speech.Object, executionService)),
            new FormVoiceCommandFieldErrorHandlingService(Telemetry.Object, _speech.Object, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy()));

        var privacy = new Mock<IFormDialogPrivacyReadbackPolicy>(MockBehavior.Loose);
        privacy.Setup(p => p.IsSpellOutAllowedInConfirmationAsync(It.IsAny<string>())).ReturnsAsync(true);
        privacy.Setup(p => p.RequiresReadbackOptInAsync(It.IsAny<string>())).ReturnsAsync(false);
        var context = SpellingWorkflowTestFactory.CreateContext("de", ["okay alex"]);
        // Ohne automatisches Weiterrücken genügt die Fortsetzungsansage.
        // Tests des Fokuswechsels schalten das Vorrücken ausdrücklich ein.
        context = context with
        {
            Selection = context.Selection with { VoiceSettings = new VoiceDialogSettings { AutoAdvanceAfterCommit = AutoAdvanceAfterCommit } }
        };
        _stateTracker = new FormVoiceCommandStateTracker(Telemetry.Object, _session);
        // Vor der Bestätigung dieselben Phasen wie im Anwendungsablauf durchlaufen:
        // Aufforderung, Zuhören, Interpretation und Validierung.
        _stateTracker.BeginFieldPrompt("main");
        _stateTracker.BeginListening();
        _stateTracker.BeginInterpretation();
        _stateTracker.BeginValidation();
        var turnContext = new FormVoiceCommandFieldTurnContext(
            context,
            _interpreter.Object,
            Field("name", "main"),
            _session,
            _stateTracker,
            new FormVoiceCommandAutoHelpTracker(
                context.VoiceSettings,
                new Mock<IFormVoiceCommandPromptService>(MockBehavior.Loose).Object,
                new Mock<IFormVoiceCommandDialogStateTracker>(MockBehavior.Loose).Object,
                _interpreter.Object,
                "de"),
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            privacy.Object);

        return await handler.HandleAsync(
            turnContext,
            new FormVoiceCommandConfirmationRequest("max", fromSpellingOrDigits: FromSpellingOrDigits),
            CancellationToken.None);
    }

    public static FormFieldSnapshot Field(string fieldId, string fieldGroupId)
        => new(
            Identity: new FormFieldIdentitySnapshot(FieldId: fieldId, VariableId: fieldId, FieldGroupId: fieldGroupId),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: fieldGroupId,
                Label: fieldId,
                HelpText: null,
                Examples: [],
                Options: []),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.String,
                SpeakValueInConfirmation: true));
}
