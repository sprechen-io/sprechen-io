using Moq;
using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, dass ein nicht möglicher Rücksprung genau eine modellierte
/// Rückmeldung anfordert und einen Navigationsfehler meldet.
/// Eine tatsächlich hörbare Wiedergabe ist nicht Bestandteil dieser Tests.
/// </summary>
public sealed class FormVoiceCommandBackBoundaryFeedbackTests
{
    [Fact]
    public async Task Back_WhenTheMoveFails_SpeaksTheModeledUnavailabilityExactlyOnce()
    {
        var telemetry = new Mock<IEvaluationFormTelemetry>();
        var speechService = CreateSpeechService();
        var interpreter = CreateInterpreter(canMoveBack: false);

        var handler = CreateHandler(telemetry.Object, speechService.Object);
        var result = await handler.HandleAsync(CreateContext(interpreter.Object, out var session), CancellationToken.None);

        Assert.Null(result);
        telemetry.Verify(
            t => t.TrackNavigationError(session, NavigationErrorCodes.CannotMoveBack, ConstraintScopes.CrossGroup),
            Times.Once);
        speechService.Verify(
            s => s.SpeakSafeAsync(
                ModeledPrompt(FormDialogPromptIds.BackUnavailable),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        speechService.Verify(
            s => s.SpeakSafeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Back_WhenTheMoveSucceeds_SaysNothingAndReportsNoNavigationError()
    {
        var telemetry = new Mock<IEvaluationFormTelemetry>();
        var speechService = CreateSpeechService();
        var interpreter = CreateInterpreter(canMoveBack: true);

        var handler = CreateHandler(telemetry.Object, speechService.Object);
        await handler.HandleAsync(CreateContext(interpreter.Object, out _), CancellationToken.None);

        speechService.Verify(
            s => s.SpeakSafeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()),
            Times.Never);
        telemetry.Verify(
            t => t.TrackNavigationError(It.IsAny<EvaluationSession>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Back_WhenTheAddressingIsInformal_SpeaksTheInformalVariant()
    {
        var speechService = CreateSpeechService();
        var interpreter = CreateInterpreter(canMoveBack: false);
        var informalSettings = new VoiceDialogSettings { PolitenessLevel = PolitenessLevel.Informal };

        var handler = CreateHandler(Mock.Of<IEvaluationFormTelemetry>(), speechService.Object);
        await handler.HandleAsync(
            CreateContext(interpreter.Object, out _, informalSettings),
            CancellationToken.None);

        var informalText = ModeledPrompt(FormDialogPromptIds.BackUnavailable + ".informal");
        Assert.NotEqual(informalText, ModeledPrompt(FormDialogPromptIds.BackUnavailable));
        speechService.Verify(
            s => s.SpeakSafeAsync(informalText, "de", informalSettings, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Back_WhenRepeated_SpeaksOncePerCommandInsteadOfScalingWithTheAttempts()
    {
        var speechService = CreateSpeechService();
        var interpreter = CreateInterpreter(canMoveBack: false);
        var handler = CreateHandler(Mock.Of<IEvaluationFormTelemetry>(), speechService.Object);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await handler.HandleAsync(CreateContext(interpreter.Object, out _), CancellationToken.None);
        }

        speechService.Verify(
            s => s.SpeakSafeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task Back_WhenTheMoveFails_KeepsTheCancellationWithTheCallerAndTouchesNoValue()
    {
        var speechService = CreateSpeechService();
        var interpreter = CreateInterpreter(canMoveBack: false);
        using var cancellation = new CancellationTokenSource();

        var handler = CreateHandler(Mock.Of<IEvaluationFormTelemetry>(), speechService.Object);
        await handler.HandleAsync(CreateContext(interpreter.Object, out _), cancellation.Token);

        speechService.Verify(
            s => s.SpeakSafeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), cancellation.Token),
            Times.Once);
        interpreter.Verify(i => i.TryMoveFocusBackAsync(), Times.Once);
        interpreter.VerifyNoOtherCalls();
    }

    private static string ModeledPrompt(string promptId)
    {
        Assert.True(
            VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(promptId, "de", out var template),
            $"Der Ansagekatalog führt '{promptId}' nicht in 'de'.");
        return template;
    }

    private static FormVoiceCommandNavigationHandler CreateHandler(
        IEvaluationFormTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService)
        => new(telemetry, Mock.Of<IFormVoiceCommandSubmissionService>(), speechService);

    private static Mock<IFormDialogRuntime> CreateInterpreter(bool canMoveBack)
    {
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter.Setup(i => i.TryMoveFocusBackAsync()).ReturnsAsync(canMoveBack);
        return interpreter;
    }

    private static Mock<IFormVoiceCommandSpeechService> CreateSpeechService()
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>();
        speechService
            .Setup(s => s.SpeakSafeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return speechService;
    }

    private static FormVoiceCommandGlobalCommandContext CreateContext(
        IFormDialogRuntime interpreter,
        out EvaluationSession session,
        VoiceDialogSettings? voiceSettings = null)
    {
        var contract = CreateMinimalContract();
        var (application, form) = CreateApplicationAndForm();
        session = new EvaluationSession("test", EvaluationChannel.Voice, "test-app", "form-1");

        return new FormVoiceCommandGlobalCommandContext(
            Request: new FormVoiceCommandGlobalCommandRequest(
                CommandId: GlobalCommandIds.Back,
                IsoCode: "de",
                Argument: null),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: voiceSettings ?? new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Runtime: new FormVoiceCommandGlobalCommandRuntime(
                Interpreter: interpreter,
                Session: session,
                StateTracker: new TestFormVoiceCommandDialogStateTracker()));
    }

    private static (ApplicationDescriptor Application, FormDescriptor Form) CreateApplicationAndForm()
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("form-1"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            Description = new LocalizedText { ["de"] = "Teil 1" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "test-app",
            DisplayName = new LocalizedText { ["de"] = "App" },
            Description = new LocalizedText { ["de"] = "App" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = [form]
        };

        return (application, form);
    }

    private static FormContract CreateMinimalContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test-app",
            FormId = FormId.From("form-1"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("name") }
                    ]
                }
            ]
        };
    }
}
