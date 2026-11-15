using Moq;
using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandSwitchFormHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenPrerequisitesMissing_SpeaksPrompt_AndReturnsBackToFormSelection()
    {
        var (application, currentForm, targetForm) = CreateApplication();
        var targetName = "Teil 2";

        var formMatcher = new Mock<IFormVoiceCommandFormMatcher>(MockBehavior.Strict);
        formMatcher
            .Setup(m => m.TryResolveFormByArgument(
                application,
                It.IsAny<ImmutableArray<FormDescriptor>>(),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                "teil 2",
                out targetForm,
                out targetName))
            .Returns(true);

        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.SpeakSafeAsync(
                It.Is<string>(text =>
                    text.Contains("Teil 2", StringComparison.Ordinal)
                    && text.Contains("Teil 1", StringComparison.Ordinal)),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.TrackNavigationError(
                It.IsAny<EvaluationSession>(),
                NavigationErrorCodes.FormPrerequisitesMissing,
                ConstraintScopes.CrossGroup));

        var handler = CreateHandler(telemetry.Object, formMatcher.Object, speechService.Object);

        var contract = CreateMinimalContract(application.ApplicationId, currentForm.FormId);
        var interpreter = new FormInterpreterRuntimeOwner(contract, requestedIsoCode: "de", new Dictionary<string, string>());

        var context = new FormVoiceCommandGlobalCommandContext(
            Request: new FormVoiceCommandGlobalCommandRequest(
                CommandId: GlobalCommandIds.SwitchForm,
                IsoCode: "de",
                Argument: "teil 2"),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: currentForm,
                Contract: contract),
            Runtime: new FormVoiceCommandGlobalCommandRuntime(
                Interpreter: interpreter,
                Session: new EvaluationSession("test", EvaluationChannel.Voice, "test-app", "form-1"),
                StateTracker: new TestFormVoiceCommandDialogStateTracker()));

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(VoiceCommandNavigationKind.BackToFormSelection, result.Navigation);
        speechService.VerifyAll();
        telemetry.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPrerequisitesMet_ConfirmsAndReturnsSwitchForm()
    {
        var (application, currentForm, targetForm) = CreateApplication();
        var targetName = "Teil 2";

        var formMatcher = new Mock<IFormVoiceCommandFormMatcher>(MockBehavior.Strict);
        formMatcher
            .Setup(m => m.TryResolveFormByArgument(
                application,
                It.IsAny<ImmutableArray<FormDescriptor>>(),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                "teil 2",
                out targetForm,
                out targetName))
            .Returns(true);

        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.ConfirmYesNoAsync(
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<string>(),
                "de",
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);

        var handler = CreateHandler(telemetry.Object, formMatcher.Object, speechService.Object);

        var contract = CreateMinimalContract(application.ApplicationId, currentForm.FormId);
        var interpreter = new FormInterpreterRuntimeOwner(contract, requestedIsoCode: "de", new Dictionary<string, string>());

        var context = new FormVoiceCommandGlobalCommandContext(
            Request: new FormVoiceCommandGlobalCommandRequest(
                CommandId: GlobalCommandIds.SwitchForm,
                IsoCode: "de",
                Argument: "teil 2"),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase, "part1"),
                Form: currentForm,
                Contract: contract),
            Runtime: new FormVoiceCommandGlobalCommandRuntime(
                Interpreter: interpreter,
                Session: new EvaluationSession("test", EvaluationChannel.Voice, "test-app", "form-1"),
                StateTracker: new TestFormVoiceCommandDialogStateTracker()));

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(VoiceCommandNavigationKind.SwitchForm, result.Navigation);
        Assert.Equal("part2", result.NavigationFormId);
        speechService.VerifyAll();
        telemetry.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenOpenApplicationsCommand_ReturnsBackToAppSelection()
    {
        var (application, currentForm, _) = CreateApplication();
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var formMatcher = new Mock<IFormVoiceCommandFormMatcher>(MockBehavior.Strict);

        var handler = CreateHandler(telemetry.Object, formMatcher.Object, speechService.Object);

        var contract = CreateMinimalContract(application.ApplicationId, currentForm.FormId);
        var interpreter = new FormInterpreterRuntimeOwner(contract, requestedIsoCode: "de", new Dictionary<string, string>());

        var context = new FormVoiceCommandGlobalCommandContext(
            Request: new FormVoiceCommandGlobalCommandRequest(
                CommandId: GlobalCommandIds.OpenApplications,
                IsoCode: "de",
                Argument: null),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase, "part1"),
                Form: currentForm,
                Contract: contract),
            Runtime: new FormVoiceCommandGlobalCommandRuntime(
                Interpreter: interpreter,
                Session: new EvaluationSession("test", EvaluationChannel.Voice, "test-app", "form-1"),
                StateTracker: new TestFormVoiceCommandDialogStateTracker()));

        var result = await handler.HandleAsync(context, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(VoiceCommandNavigationKind.BackToAppSelection, result.Navigation);
        speechService.VerifyNoOtherCalls();
        telemetry.VerifyNoOtherCalls();
        formMatcher.VerifyNoOtherCalls();
    }

    private static (ApplicationDescriptor Application, FormDescriptor CurrentForm, FormDescriptor TargetForm) CreateApplication()
    {
        var part1 = new FormDescriptor
        {
            FormId = FormId.From("part1"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            Description = new LocalizedText { ["de"] = "Teil 1" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var part2 = new FormDescriptor
        {
            FormId = FormId.From("part2"),
            DisplayName = new LocalizedText { ["de"] = "Teil 2" },
            Description = new LocalizedText { ["de"] = "Teil 2" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"],
            PrerequisiteForms = ["part1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "App" },
            Description = new LocalizedText { ["de"] = "App" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = [part1, part2]
        };

        return (application, part1, part2);
    }

    private static FormVoiceCommandSwitchFormHandler CreateHandler(
        IEvaluationFormTelemetry telemetry,
        IFormVoiceCommandFormMatcher formMatcher,
        IFormVoiceCommandSpeechService speechService)
        => new(
            new FormVoiceCommandSwitchFormNavigationExit(telemetry, speechService),
            new FormVoiceCommandSwitchFormTargetResolver(telemetry, formMatcher, speechService),
            new FormVoiceCommandSwitchFormPrerequisiteGuard(
                telemetry,
                speechService,
                new FormVoiceCommandSwitchFormPrerequisiteResolver()),
            new FormVoiceCommandSwitchFormConfirmationFlow(speechService));

    private static FormContract CreateMinimalContract(string applicationId, string formId)
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = applicationId,
            FormId = FormId.From(formId),
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
