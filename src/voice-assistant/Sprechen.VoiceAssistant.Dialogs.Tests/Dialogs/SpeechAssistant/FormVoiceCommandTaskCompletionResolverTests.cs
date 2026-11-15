using System.Collections.Immutable;
using Moq;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandTaskCompletionResolverTests
{
    [Fact]
    public async Task ResolveAsync_WithoutARuntime_IsUnknownButKeepsTheClassification()
    {
        var resolver = new FormVoiceCommandTaskCompletionResolver();

        var completion = await resolver.ResolveAsync(runtime: null, Submitted(), technicalFailure: false);

        Assert.Equal(EvaluationTaskCompletion.Unknown with { TerminationClassification = ParticipantTaskTerminationClassification.Completed }, completion);
    }

    [Fact]
    public async Task ResolveAsync_WhenTheStatusCannotBeRead_IsUnknownWithATechnicalFailure()
    {
        var runtime = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        runtime.Setup(item => item.GetStatusSnapshotAsync()).ThrowsAsync(new InvalidOperationException("status unavailable"));
        var resolver = new FormVoiceCommandTaskCompletionResolver();

        var completion = await resolver.ResolveAsync(runtime.Object, Submitted(), technicalFailure: false);

        Assert.Equal(EvaluationTaskCompletion.Unknown with { TerminationClassification = ParticipantTaskTerminationClassification.TechnicalFailure }, completion);
    }

    [Fact]
    public async Task ResolveAsync_WithAReadableStatus_CountsRequiredFieldsAndOpenValidationErrors()
    {
        var runtime = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        runtime.Setup(item => item.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(requiredCompleted: 3, requiredTotal: 5, validationErrors: 2));
        SetupEmptyComparison(runtime);
        var resolver = new FormVoiceCommandTaskCompletionResolver();

        var completion = await resolver.ResolveAsync(runtime.Object, Submitted(), technicalFailure: false);

        Assert.Equal(new EvaluationTaskCompletion(5, 3, 2, ParticipantTaskTerminationClassification.Completed, TelemetryMismatchCodes.MissingRequired), completion);
    }

    [Fact]
    public async Task ResolveAsync_ClampsNegativeCountsToZero()
    {
        var runtime = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        runtime.Setup(item => item.GetStatusSnapshotAsync()).ReturnsAsync(CreateStatusSnapshot(requiredCompleted: -1, requiredTotal: -4, validationErrors: -2));
        SetupEmptyComparison(runtime);
        var resolver = new FormVoiceCommandTaskCompletionResolver();

        var completion = await resolver.ResolveAsync(runtime.Object, Abandoned(), technicalFailure: false);

        Assert.Equal(new EvaluationTaskCompletion(0, 0, 0, ParticipantTaskTerminationClassification.SessionAbandoned, TelemetryMismatchCodes.NotSubmitted), completion);
    }

    [Fact]
    public void Classify_ATechnicalFailure_OutranksTheDialogOutcome()
    {
        var resolver = new FormVoiceCommandTaskCompletionResolver();

        Assert.Equal(ParticipantTaskTerminationClassification.TechnicalFailure, resolver.Classify(Submitted(), technicalFailure: true));
        Assert.Equal(ParticipantTaskTerminationClassification.TechnicalFailure, resolver.Classify(Cancelled(), technicalFailure: true));
    }

    [Fact]
    public void Classify_ASubmittedForm_IsCompleted()
    {
        var resolver = new FormVoiceCommandTaskCompletionResolver();

        Assert.Equal(ParticipantTaskTerminationClassification.Completed, resolver.Classify(Submitted(), technicalFailure: false));
    }

    [Fact]
    public void Classify_ACancelledAbort_IsParticipantCancelled()
    {
        var resolver = new FormVoiceCommandTaskCompletionResolver();

        Assert.Equal(ParticipantTaskTerminationClassification.ParticipantCancelled, resolver.Classify(Cancelled(), technicalFailure: false));
    }

    [Fact]
    public void Classify_AnyOtherEnd_IsSessionAbandoned()
    {
        var resolver = new FormVoiceCommandTaskCompletionResolver();

        Assert.Equal(ParticipantTaskTerminationClassification.SessionAbandoned, resolver.Classify(Abandoned(), technicalFailure: false));
        Assert.Equal(
            ParticipantTaskTerminationClassification.SessionAbandoned,
            resolver.Classify(new FormVoiceCommandSessionRunResult(EvaluationOutcome.Aborted, null, FormSurfaceStage.Abandoned, AbortReason: null), technicalFailure: false));
    }

    private static FormVoiceCommandSessionRunResult Submitted()
        => new(EvaluationOutcome.Submitted, null, FormSurfaceStage.Submitted);

    private static FormVoiceCommandSessionRunResult Cancelled()
        => new(EvaluationOutcome.Aborted, null, FormSurfaceStage.Abandoned, EvaluationAbortReason.Cancelled);

    private static FormVoiceCommandSessionRunResult Abandoned()
        => FormVoiceCommandSessionRunResult.Abandoned();

    private static void SetupEmptyComparison(Mock<IFormDialogRuntime> runtime)
    {
        runtime.SetupGet(item => item.Contract).Returns(new FormContract());
        runtime.Setup(item => item.GetDetailedStatusSnapshotAsync())
            .ReturnsAsync(new FormDetailedStatusSnapshot([], []));
        runtime.Setup(item => item.GetValuesSnapshotAsync())
            .ReturnsAsync(ImmutableDictionary<string, string>.Empty);
    }

    private static FormStatusSnapshot CreateStatusSnapshot(int requiredCompleted, int requiredTotal, int validationErrors)
        => new(
            new FormStatusNavigationPosition(
                CurrentFormId: "current",
                CurrentFormIndex: 1,
                TotalForms: 1,
                CurrentFieldGroupId: "main",
                CurrentFieldGroupIndex: 1,
                TotalFieldGroups: 1,
                CurrentFieldId: "field"),
            new FormStatusCompletionProgress(
                new FormStatusProgressDimension(Completed: requiredCompleted, Total: requiredTotal),
                new FormStatusProgressDimension(Completed: requiredCompleted, Total: requiredTotal),
                new FormStatusProgressDimension(Completed: 0, Total: 1)),
            CurrentFieldLabel: "Field",
            CurrentFieldIndex: 1,
            CurrentFieldGroupTitle: "Main",
            OpenRequiredFieldCount: Math.Max(0, requiredTotal - requiredCompleted),
            ValidationErrorCount: validationErrors);
}
