using System.Collections.Immutable;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandSubmissionServiceTests
{
    [Fact]
    public async Task TrySubmitAsync_NoViolations_ReturnsSubmitted()
    {
        var interpreter = CreateInterpreter(violations: ImmutableArray<FormConstraintViolation>.Empty, focusableError: null);
        var sut = CreateService();

        var result = await Invoke(sut, interpreter);

        Assert.Equal(FormSubmissionResult.Submitted, result);
    }

    [Fact]
    public async Task TrySubmitAsync_NoViolationsAndNoSubmitAttemptPrompts_DoesNotSpeakBeforeTerminalAcceptance()
    {
        var interpreter = CreateInterpreter(violations: ImmutableArray<FormConstraintViolation>.Empty, focusableError: null);
        var spokenTexts = new List<string>();
        var sut = CreateService(spokenTexts);

        var result = await Invoke(sut, interpreter);

        Assert.Equal(FormSubmissionResult.Submitted, result);
        Assert.Empty(spokenTexts);
    }

    [Fact]
    public async Task TrySubmitAsync_NoViolationsButContractSpeaksOnSubmit_DoesNotSpeakBeforeTerminalAcceptance()
    {
        var interpreter = CreateInterpreter(
            violations: ImmutableArray<FormConstraintViolation>.Empty,
            focusableError: null,
            submitAttemptPromptIds: ImmutableArray.Create("contract.onSubmit.thanks"));
        var spokenTexts = new List<string>();
        var sut = CreateService(spokenTexts);

        var result = await Invoke(sut, interpreter);

        Assert.Equal(FormSubmissionResult.Submitted, result);
        Assert.Empty(spokenTexts);
    }

    [Fact]
    public async Task TrySubmitAsync_BlockedSubmission_DoesNotSpeakCompletionAcknowledgment()
    {
        var violation = CreateViolation();
        var violations = ImmutableArray.Create(violation);
        var interpreter = CreateInterpreter(violations, focusableError: violation);
        var spokenTexts = new List<string>();
        var sut = CreateService(spokenTexts);

        var result = await Invoke(sut, interpreter);

        Assert.Equal(FormSubmissionResult.BlockedAwaitingCorrection, result);
        Assert.DoesNotContain("Das Formular wurde abgesendet.", spokenTexts);
    }

    [Fact]
    public async Task TrySubmitAsync_ViolationWithNoFocusableErrorField_ReturnsBlockedUnresolvable()
    {
        var violations = ImmutableArray.Create(CreateViolation());
        var interpreter = CreateInterpreter(violations, focusableError: null);
        var sut = CreateService();

        var result = await Invoke(sut, interpreter);

        Assert.Equal(FormSubmissionResult.BlockedUnresolvable, result);
    }

    [Fact]
    public async Task TrySubmitAsync_ViolationWithNoFocusableErrorField_SpeaksAndTracksTheViolation()
    {
        var violation = CreateViolation(scope: ConstraintScopes.CrossGroup, fieldGroupId: null);
        var violations = ImmutableArray.Create(violation);
        var interpreter = CreateInterpreter(violations, focusableError: null);
        var spokenTexts = new List<string>();
        var telemetry = new Mock<IEvaluationFormTelemetry>();
        var sut = CreateService(spokenTexts, telemetry);

        var result = await Invoke(sut, interpreter);

        Assert.Equal(FormSubmissionResult.BlockedUnresolvable, result);
        Assert.NotEmpty(spokenTexts);
        Assert.DoesNotContain("Das Formular wurde abgesendet.", spokenTexts);
        telemetry.Verify(
            item => item.TrackConstraintViolation(
                It.IsAny<EvaluationSession>(),
                "field",
                violation.Code,
                violation.Severity,
                violation.Scope,
                violation.FieldGroupId,
                TelemetryErrorTriggers.SubmitAttempt),
            Times.Once);
    }

    [Fact]
    public async Task TrySubmitAsync_WithSeveralOpenErrors_ReportsEveryOneNotOnlyTheSpokenOne()
    {
        var first = CreateViolation(fieldId: "first", code: "REQUIRED");
        var second = CreateViolation(fieldId: "second", code: "REQUIRED");
        var third = CreateViolation(fieldId: "third", code: "PATTERN");
        var violations = ImmutableArray.Create(first, second, third);
        var interpreter = CreateInterpreter(violations, focusableError: first);
        var telemetry = new Mock<IEvaluationFormTelemetry>();
        var sut = CreateService(telemetry: telemetry);

        await Invoke(sut, interpreter);

        telemetry.Verify(
            item => item.TrackConstraintViolation(
                It.IsAny<EvaluationSession>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                TelemetryErrorTriggers.SubmitAttempt),
            Times.Exactly(3));
    }

    [Fact]
    public async Task TrySubmitAsync_SpeaksOneErrorEvenWhenItReportsSeveral()
    {
        var first = CreateViolation(fieldId: "first", code: "REQUIRED");
        var second = CreateViolation(fieldId: "second", code: "REQUIRED");
        var violations = ImmutableArray.Create(first, second);
        var interpreter = CreateInterpreter(violations, focusableError: first);
        var spokenTexts = new List<string>();
        var sut = CreateService(spokenTexts);

        await Invoke(sut, interpreter);

        Assert.Single(spokenTexts);
    }

    [Fact]
    public async Task TrySubmitAsync_CountsTheRepairAttemptOnceForTheFieldItLeadsTo()
    {
        var first = CreateViolation(fieldId: "first", code: "REQUIRED");
        var second = CreateViolation(fieldId: "second", code: "REQUIRED");
        var violations = ImmutableArray.Create(first, second);
        var interpreter = CreateInterpreter(violations, focusableError: first);
        var telemetry = new Mock<IEvaluationFormTelemetry>();
        var sut = CreateService(telemetry: telemetry);

        await Invoke(sut, interpreter);

        telemetry.Verify(
            item => item.TrackRepairAttempt(It.IsAny<EvaluationSession>(), "first", "required"),
            Times.Once);
        telemetry.Verify(
            item => item.TrackRepairAttempt(It.IsAny<EvaluationSession>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task TrySubmitAsync_ViolationWithFocusableErrorField_ReturnsBlockedAwaitingCorrection()
    {
        var violation = CreateViolation();
        var violations = ImmutableArray.Create(violation);
        var interpreter = CreateInterpreter(violations, focusableError: violation);
        var sut = CreateService();

        var result = await Invoke(sut, interpreter);

        Assert.Equal(FormSubmissionResult.BlockedAwaitingCorrection, result);
    }

    [Fact]
    public async Task TrySubmitAsync_CrossGroupErrorViolation_BlocksSubmission()
    {
        // Auch eine gruppenübergreifende Regelverletzung mit Schweregrad error blockiert das Absenden.
        var violation = CreateViolation(scope: ConstraintScopes.CrossGroup, fieldGroupId: null);
        var violations = ImmutableArray.Create(violation);
        var interpreter = CreateInterpreter(violations, focusableError: violation);
        var sut = CreateService();

        var result = await Invoke(sut, interpreter);

        Assert.NotEqual(FormSubmissionResult.Submitted, result);
        Assert.Equal(FormSubmissionResult.BlockedAwaitingCorrection, result);
    }

    private static Task<FormSubmissionResult> Invoke(
        IFormVoiceCommandSubmissionService sut,
        Mock<IFormDialogRuntime> interpreter)
    {
        var session = new EvaluationSession("p", EvaluationChannel.Voice, "app", "form");
        var stateTracker = new TestFormVoiceCommandDialogStateTracker();
        return sut.TrySubmitAsync(
            interpreter.Object,
            CreateContract(),
            session,
            "de",
            new VoiceDialogSettings(),
            stateTracker,
            CancellationToken.None);
    }

    private static Mock<IFormDialogRuntime> CreateInterpreter(
        ImmutableArray<FormConstraintViolation> violations,
        FormConstraintViolation? focusableError,
        ImmutableArray<string>? submitAttemptPromptIds = null)
    {
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter
            .Setup(item => item.TryApplySubmitAttemptRulesAsync())
            .ReturnsAsync(submitAttemptPromptIds ?? ImmutableArray<string>.Empty);
        interpreter
            .Setup(item => item.ValidateAllAsync())
            .ReturnsAsync(violations);
        interpreter
            .Setup(item => item.TryFocusNextErrorAsync())
            .ReturnsAsync(focusableError);
        interpreter
            .Setup(item => item.GetFocusedFieldAsync())
            .ReturnsAsync(CreateFocusedField());
        return interpreter;
    }

    private static FormFieldSnapshot CreateFocusedField()
        => new(
            Identity: new FormFieldIdentitySnapshot(
                FieldId: "field",
                VariableId: "field",
                FieldGroupId: "main"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Feld",
                HelpText: null,
                Examples: [],
                Options: []),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.String,
                SpeakValueInConfirmation: false));

    private static IFormVoiceCommandSubmissionService CreateService(
        List<string>? spokenTexts = null,
        Mock<IEvaluationFormTelemetry>? telemetry = null)
    {
        telemetry ??= new Mock<IEvaluationFormTelemetry>();
        var speechService = new Mock<IFormVoiceCommandSpeechService>();
        speechService
            .Setup(item => item.SpeakSafeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, VoiceDialogSettings, CancellationToken>((text, _, _, _) => spokenTexts?.Add(text))
            .Returns(Task.CompletedTask);

        return new FormVoiceCommandSubmissionService(
            telemetry.Object,
            speechService.Object);
    }

    private static FormConstraintViolation CreateViolation(
        string scope = "field",
        string? fieldGroupId = "group",
        string fieldId = "field",
        string? code = null)
        => new(
            FieldId: fieldId,
            ConstraintId: scope == ConstraintScopes.CrossGroup ? "endAfterStart" : "required",
            Severity: "error",
            Code: code ?? (scope == ConstraintScopes.CrossGroup ? "CROSS_GROUP_ORDER" : "REQUIRED"),
            Message: "Pflichtfeld",
            Remediation: null,
            Scope: scope,
            FieldGroupId: fieldGroupId);

    private static FormContract CreateContract()
        => new()
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
        };
}
