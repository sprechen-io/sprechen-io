using Moq;
using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Telemetrieaufrufe für <c>nextError</c> und <c>nextOpenRequiredField</c> bei simuliertem Navigationsergebnis.
/// Ohne Ziel wird <c>TrackNavigationAidNoTarget</c> einmal aufgerufen, bei gefundenem Ziel nicht.
/// </summary>
public sealed class FormVoiceCommandNavigationAidNoTargetTelemetryTests
{
    [Fact]
    public async Task NextError_WhenNoOpenError_EmitsNavigationAidNoTargetOnce()
    {
        var telemetry = new Mock<IEvaluationFormTelemetry>();
        var speechService = CreateSpeechService();

        var interpreter = new Mock<IFormDialogRuntime>();
        interpreter
            .Setup(i => i.TryFocusNextErrorAsync())
            .ReturnsAsync((FormConstraintViolation?)null);

        var handler = new FormVoiceCommandErrorHandler(telemetry.Object, speechService.Object);
        var context = CreateContext(interpreter.Object, GlobalCommandIds.NextError, out var session);

        await handler.HandleAsync(context, CancellationToken.None);

        telemetry.Verify(t => t.TrackNavigationAidNoTarget(session, GlobalCommandIds.NextError), Times.Once);
    }

    [Fact]
    public async Task NextError_WhenErrorFound_NeverEmitsNavigationAidNoTarget()
    {
        var telemetry = new Mock<IEvaluationFormTelemetry>();
        var speechService = CreateSpeechService();

        var interpreter = new Mock<IFormDialogRuntime>();
        interpreter
            .Setup(i => i.TryFocusNextErrorAsync())
            .ReturnsAsync(new FormConstraintViolation(
                FieldId: "name",
                ConstraintId: "required",
                Severity: "error",
                Code: "REQUIRED",
                Message: "Pflichtfeld",
                Remediation: null,
                Scope: "field",
                FieldGroupId: null));

        var handler = new FormVoiceCommandErrorHandler(telemetry.Object, speechService.Object);
        var context = CreateContext(interpreter.Object, GlobalCommandIds.NextError, out _);

        await handler.HandleAsync(context, CancellationToken.None);

        telemetry.Verify(
            t => t.TrackNavigationAidNoTarget(It.IsAny<EvaluationSession>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task NextOpenRequiredField_WhenNoOpenRequiredField_EmitsNavigationAidNoTargetOnce()
    {
        var telemetry = new Mock<IEvaluationFormTelemetry>();
        var speechService = CreateSpeechService();

        var interpreter = new Mock<IFormDialogRuntime>();
        interpreter
            .Setup(i => i.TryFocusNextOpenRequiredFieldAsync())
            .ReturnsAsync(new FocusNextOpenRequiredFieldResult(
                FocusNextOpenRequiredFieldResultKind.NoOpenRequiredField,
                TargetFieldId: null,
                RemainingOpenRequiredCount: 0));

        var handler = new FormVoiceCommandNextOpenRequiredFieldHandler(telemetry.Object, speechService.Object);
        var context = CreateContext(interpreter.Object, GlobalCommandIds.NextOpenRequiredField, out var session);

        await handler.HandleAsync(context, CancellationToken.None);

        telemetry.Verify(t => t.TrackNavigationAidNoTarget(session, GlobalCommandIds.NextOpenRequiredField), Times.Once);
    }

    [Fact]
    public async Task NextOpenRequiredField_WhenMoved_NeverEmitsNavigationAidNoTarget()
    {
        var telemetry = new Mock<IEvaluationFormTelemetry>();
        var speechService = CreateSpeechService();

        var interpreter = new Mock<IFormDialogRuntime>();
        interpreter
            .Setup(i => i.TryFocusNextOpenRequiredFieldAsync())
            .ReturnsAsync(new FocusNextOpenRequiredFieldResult(
                FocusNextOpenRequiredFieldResultKind.Moved,
                TargetFieldId: "name",
                RemainingOpenRequiredCount: 1));
        interpreter
            .Setup(i => i.GetFocusedFieldAsync())
            .ReturnsAsync(CreateFieldSnapshot());

        var handler = new FormVoiceCommandNextOpenRequiredFieldHandler(telemetry.Object, speechService.Object);
        var context = CreateContext(interpreter.Object, GlobalCommandIds.NextOpenRequiredField, out _);

        await handler.HandleAsync(context, CancellationToken.None);

        telemetry.Verify(
            t => t.TrackNavigationAidNoTarget(It.IsAny<EvaluationSession>(), It.IsAny<string>()),
            Times.Never);
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
        string commandId,
        out EvaluationSession session)
    {
        var contract = CreateMinimalContract();
        var (application, form) = CreateApplicationAndForm();
        session = new EvaluationSession("test", EvaluationChannel.Voice, "test-app", "form-1");

        return new FormVoiceCommandGlobalCommandContext(
            Request: new FormVoiceCommandGlobalCommandRequest(
                CommandId: commandId,
                IsoCode: "de",
                Argument: null),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
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

    private static FormFieldSnapshot CreateFieldSnapshot()
    {
        return new FormFieldSnapshot(
            new FormFieldIdentitySnapshot("name", "name", "main"),
            new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Name",
                HelpText: null,
                Examples: [],
                Options: []),
            new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: "string",
                SpeakValueInConfirmation: true));
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
