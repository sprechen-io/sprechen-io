using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Contracts.FormSurfaces;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandSessionRunnerTests
{
    [Theory]
    [InlineData("app", "current", "1")]
    [InlineData("other-app", "requested-form", "2")]
    [InlineData("other-app", null, "3")]
    public async Task RunAsync_BindsTelemetryToLoadedFormIndependentlyOfRequestAndVersion(
        string applicationId, string? requestedFormId, string contractVersion)
    {
        var scenario = CreateScenario();
        scenario.Context.Contract.ContractVersion = contractVersion;
        var context = scenario.Context with
        {
            SessionIdentity = scenario.Context.SessionIdentity with
            {
                ApplicationId = applicationId,
                FormId = requestedFormId,
            },
        };

        await scenario.Runner.RunAsync(context, CancellationToken.None);

        scenario.Telemetry.Verify(
            telemetry => telemetry.StartSessionAsync(
                applicationId, "current", "de", EvaluationChannel.Voice, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_UninitializedLoadedFormIdCannotStartTelemetry()
    {
        var scenario = CreateScenario();
        scenario.Context.Contract.FormId = default;
        Assert.NotEmpty(scenario.Context.Contract.Fields);
        Assert.NotEmpty(scenario.Context.Contract.FieldGroups);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => scenario.Runner.RunAsync(scenario.Context, CancellationToken.None));

        scenario.Telemetry.Verify(
            telemetry => telemetry.StartSessionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<EvaluationChannel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }


    [Fact]
    public async Task RunAsync_WhenFieldWorkPausedMidDialog_ReturnsPausedResultWithHarvestedValues()
    {
        var scenario = CreateScenario();

        var result = await scenario.Runner.RunAsync(scenario.Context, CancellationToken.None);

        Assert.True(result.WasPaused);
        Assert.Equal("app", result.ApplicationId);
        Assert.Equal("current", result.FormId);
        Assert.Equal("Max", result.ValuesByVariableId["name"]);
    }

    [Fact]
    public async Task RunAsync_WhenFieldWorkPausedMidDialog_StillEndsTelemetrySessionInFinally()
    {
        var scenario = CreateScenario();

        await scenario.Runner.RunAsync(scenario.Context, CancellationToken.None);

        scenario.Telemetry.Verify(
            t => t.EndSessionAsync(
                scenario.Session,
                It.IsAny<EvaluationOutcome>(),
                It.IsAny<EvaluationTaskCompletion>(),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenRuntimeCreateRejectsInvalidPolicy_EndsSessionWithoutSpokenFailureOrPublication()
    {
        var scenario = CreateScenario(
            promptException: null,
            runtimeCreateException: new FormContractPolicyInvalidException(
                EffectiveSensitiveDataPolicyFailure.UnknownPrivacyClass));

        var thrown = await Assert.ThrowsAsync<FormContractPolicyInvalidException>(
            () => scenario.Runner.RunAsync(scenario.Context, CancellationToken.None));

        Assert.Equal(EffectiveSensitiveDataPolicyFailure.UnknownPrivacyClass, thrown.Failure);
        scenario.Telemetry.Verify(
            t => t.EndSessionAsync(
                scenario.Session,
                It.IsAny<EvaluationOutcome>(),
                It.IsAny<EvaluationTaskCompletion>(),
                CancellationToken.None),
            Times.Once);
        scenario.SpeechCaptureFeedback.Verify(
            feedback => feedback.ReportFailureAsync(
                It.IsAny<SpeechErrorCode>(),
                It.IsAny<string>()),
            Times.Never);
        scenario.SurfaceController.Verify(
            controller => controller.BeginSession(),
            Times.Never);
        scenario.SurfaceController.Verify(
            controller => controller.TryPublishActive(
                It.IsAny<FormSurfacePublicationSession>(),
                It.IsAny<SpeechAssistantFormSurfaceView>()),
            Times.Never);
        scenario.SurfaceController.Verify(
            controller => controller.TryPublishTerminal(
                It.IsAny<FormSurfacePublicationSession>(),
                It.IsAny<SpeechAssistantFormSurfaceView>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenRuntimeCreationFailsTechnically_AnnouncesOnlyAfterAcceptanceAndCommitWithoutSurface()
    {
        var terminalAcceptance = new TaskCompletionSource<EvaluationSessionAcceptance?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var localCommit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completionObserved = new TaskCompletionSource<EvaluationTaskCompletion>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var commitObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scenario = CreateScenario(
            runtimeCreateException: new InvalidOperationException("runtime failed"),
            telemetryEndTask: terminalAcceptance.Task,
            telemetryCommitTask: localCommit.Task,
            completionObserver: completion => completionObserved.TrySetResult(completion),
            commitObserver: () => commitObserved.TrySetResult());

        var runTask = scenario.Runner.RunAsync(scenario.Context, CancellationToken.None);
        var completion = await completionObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(ParticipantTaskTerminationClassification.TechnicalFailure, completion.TerminationClassification);
        Assert.Empty(scenario.TerminalSpeech.Invocations);
        Assert.Empty(scenario.Earcon.Invocations);
        scenario.SurfaceController.Verify(controller => controller.BeginSession(), Times.Never);

        terminalAcceptance.SetResult(CreateAcceptance());
        await commitObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(runTask.IsCompleted);
        Assert.Empty(scenario.TerminalSpeech.Invocations);
        Assert.Empty(scenario.Earcon.Invocations);

        localCommit.SetResult();
        var result = await runTask;

        Assert.True(result.WasAbandoned);
        var speech = Assert.Single(scenario.TerminalSpeech.Invocations);
        Assert.Equal(
            FormDialogFlowPromptTextResolver.GetEvaluationTechnicalFailurePrompt("de"),
            speech.Arguments[0]);
        Assert.Empty(scenario.Earcon.Invocations);
        scenario.SurfaceController.Verify(
            controller => controller.TryPublishTerminal(
                It.IsAny<FormSurfacePublicationSession>(),
                It.IsAny<SpeechAssistantFormSurfaceView>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenFieldWorkFailsMidDialog_ReturnsAbandonedResultWithHarvestedValues()
    {
        var scenario = CreateScenario(new InvalidOperationException("boom"));

        var result = await scenario.Runner.RunAsync(scenario.Context, CancellationToken.None);

        Assert.True(result.WasAbandoned);
        Assert.Equal("app", result.ApplicationId);
        Assert.Equal("current", result.FormId);
        Assert.Equal("Max", result.ValuesByVariableId["name"]);
        scenario.SpeechCaptureFeedback.Verify(
            feedback => feedback.ReportFailureAsync(
                SpeechErrorCode.VoiceCommandDialogFailed,
                "Voice command dialog failed."),
            Times.Never);
        scenario.TerminalSpeech.Verify(
            speech => speech.SpeakSafeAsync(
                FormDialogFlowPromptTextResolver.GetEvaluationTechnicalFailurePrompt("de"),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_TerminalResultAndSurfaceWaitForTelemetryTerminalAcceptance()
    {
        var terminalAcceptance = new TaskCompletionSource<EvaluationSessionAcceptance?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completionObserved = new TaskCompletionSource<EvaluationTaskCompletion>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var scenario = CreateScenario(
            new InvalidOperationException("boom"),
            telemetryEndTask: terminalAcceptance.Task,
            completionObserver: completion => completionObserved.TrySetResult(completion));

        var runTask = scenario.Runner.RunAsync(scenario.Context, CancellationToken.None);
        _ = await completionObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(runTask.IsCompleted);
        scenario.SurfaceController.Verify(
            controller => controller.TryPublishTerminal(
                It.IsAny<FormSurfacePublicationSession>(),
                It.IsAny<SpeechAssistantFormSurfaceView>()),
            Times.Never);

        terminalAcceptance.SetResult(CreateAcceptance());
        var result = await runTask;

        Assert.True(result.WasAbandoned);
        scenario.SurfaceController.Verify(
            controller => controller.TryPublishTerminal(
                It.IsAny<FormSurfacePublicationSession>(),
                It.IsAny<SpeechAssistantFormSurfaceView>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_SubmittedTimelineEmitsNoAuditoryOrVisualSuccessBeforeTaskEnd204()
    {
        var terminalAcceptance = new TaskCompletionSource<EvaluationSessionAcceptance?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var completionObserved = new TaskCompletionSource<EvaluationTaskCompletion>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var scenario = CreateScenario(
            telemetryEndTask: terminalAcceptance.Task,
            completionObserver: completion => completionObserved.TrySetResult(completion),
            successfulSubmission: true);

        var runTask = scenario.Runner.RunAsync(scenario.Context, CancellationToken.None);
        var completion = await completionObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(ParticipantTaskTerminationClassification.Completed, completion.TerminationClassification);
        Assert.False(runTask.IsCompleted);
        scenario.TerminalSpeech.Verify(
            speech => speech.SpeakSafeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        scenario.Earcon.Verify(
            earcon => earcon.PlayAsync(EarconId.FlowDone, It.IsAny<CancellationToken>()),
            Times.Never);
        scenario.SurfaceController.Verify(
            controller => controller.TryPublishTerminal(
                It.IsAny<FormSurfacePublicationSession>(),
                It.IsAny<SpeechAssistantFormSurfaceView>()),
            Times.Never);

        terminalAcceptance.SetResult(CreateAcceptance());
        var result = await runTask;

        Assert.True(result.WasSubmitted);
        scenario.TerminalSpeech.Verify(
            speech => speech.SpeakSafeAsync(
                FormDialogFlowPromptTextResolver.GetFormSubmittedPrompt("de"),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                CancellationToken.None),
            Times.Once);
        scenario.Earcon.Verify(
            earcon => earcon.PlayAsync(EarconId.FlowDone, CancellationToken.None),
            Times.Once);
        scenario.SurfaceController.Verify(
            controller => controller.TryPublishTerminal(
                It.IsAny<FormSurfacePublicationSession>(),
                It.IsAny<SpeechAssistantFormSurfaceView>()),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_ConfirmedCancellationSpeaksOnlyAfterAcceptanceAndLocalCommitWithoutCompletionCue()
    {
        var terminalAcceptance = new TaskCompletionSource<EvaluationSessionAcceptance?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var localCommit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completionObserved = new TaskCompletionSource<EvaluationTaskCompletion>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var commitObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scenario = CreateScenario(
            telemetryEndTask: terminalAcceptance.Task,
            telemetryCommitTask: localCommit.Task,
            completionObserver: completion => completionObserved.TrySetResult(completion),
            commitObserver: () => commitObserved.TrySetResult(),
            cancelled: true);

        var runTask = scenario.Runner.RunAsync(scenario.Context, CancellationToken.None);
        var completion = await completionObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(ParticipantTaskTerminationClassification.ParticipantCancelled, completion.TerminationClassification);
        Assert.False(runTask.IsCompleted);
        Assert.Empty(scenario.TerminalSpeech.Invocations);
        Assert.Empty(scenario.Earcon.Invocations);

        terminalAcceptance.SetResult(CreateAcceptance());
        await commitObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(runTask.IsCompleted);
        Assert.Empty(scenario.TerminalSpeech.Invocations);
        Assert.Empty(scenario.Earcon.Invocations);

        localCommit.SetResult();
        var result = await runTask;

        Assert.True(result.WasCancelled);
        var speech = Assert.Single(scenario.TerminalSpeech.Invocations);
        Assert.Equal(nameof(IFormVoiceCommandSpeechService.SpeakSafeAsync), speech.Method.Name);
        Assert.Equal(
            FormDialogFlowPromptTextResolver.GetEvaluationTaskCancelledPrompt("de"),
            speech.Arguments[0]);
        Assert.Empty(scenario.Earcon.Invocations);
    }

    [Fact]
    public async Task RunAsync_WhenEvaluationIsSuppressed_ReportsOnlyTheCancellationOutcomeWithoutACommitClaim()
    {
        var scenario = CreateScenario(
            telemetryEndTask: Task.FromResult<EvaluationSessionAcceptance?>(null),
            cancelled: true);

        var result = await scenario.Runner.RunAsync(scenario.Context, CancellationToken.None);

        Assert.True(result.WasCancelled);
        scenario.Telemetry.Verify(
            telemetry => telemetry.CommitSessionAsync(
                It.IsAny<EvaluationSessionAcceptance>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        var speech = Assert.Single(scenario.TerminalSpeech.Invocations);
        Assert.Equal(
            FormDialogFlowPromptTextResolver.GetEvaluationTaskCancelledPrompt("de"),
            speech.Arguments[0]);
        Assert.DoesNotContain("gespeichert", Assert.IsType<string>(speech.Arguments[0]), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(scenario.Earcon.Invocations);
    }

    [Fact]
    public async Task RunAsync_WhenUnknownTechnicalFaultEscapesFieldWork_RecordsTechnicalFailure()
    {
        var completionObserved = new TaskCompletionSource<EvaluationTaskCompletion>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var scenario = CreateScenario(
            new InvalidOperationException("boom"),
            completionObserver: completion => completionObserved.TrySetResult(completion));

        var result = await scenario.Runner.RunAsync(scenario.Context, CancellationToken.None);
        var completion = await completionObserved.Task;

        Assert.True(result.WasAbandoned);
        Assert.Equal(ParticipantTaskTerminationClassification.TechnicalFailure, completion.TerminationClassification);
        var speech = Assert.Single(scenario.TerminalSpeech.Invocations);
        Assert.Equal(
            FormDialogFlowPromptTextResolver.GetEvaluationTechnicalFailurePrompt("de"),
            speech.Arguments[0]);
        Assert.Empty(scenario.Earcon.Invocations);
    }

    [Fact]
    public async Task RunAsync_WhenTelemetryDeliveryFailsBeforeAcceptance_DoesNotPublishTerminal()
    {
        var scenario = CreateScenario(
            new InvalidOperationException("boom"),
            telemetryEndTask: Task.FromException<EvaluationSessionAcceptance?>(new IOException("delivery failed")));

        await Assert.ThrowsAsync<IOException>(
            () => scenario.Runner.RunAsync(scenario.Context, CancellationToken.None));

        scenario.SurfaceController.Verify(
            controller => controller.TryPublishTerminal(
                It.IsAny<FormSurfacePublicationSession>(),
                It.IsAny<SpeechAssistantFormSurfaceView>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenTerminalDeliveryIsPermanentlyRejected_AnnouncesActionableFailure()
    {
        var scenario = CreateScenario(
            telemetryEndTask: Task.FromException<EvaluationSessionAcceptance?>(
                new EvaluationTelemetryDeliveryRejectedException(422)));

        await Assert.ThrowsAsync<EvaluationTelemetryDeliveryRejectedException>(
            () => scenario.Runner.RunAsync(scenario.Context, CancellationToken.None));

        scenario.TerminalSpeech.Verify(
            speech => speech.SpeakSafeAsync(
                FormDialogFlowPromptTextResolver.GetEvaluationPermanentFailurePrompt("de"),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                CancellationToken.None),
            Times.Once);
        scenario.SurfaceController.Verify(
            controller => controller.TryPublishTerminal(
                It.IsAny<FormSurfacePublicationSession>(),
                It.IsAny<SpeechAssistantFormSurfaceView>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenRestoredCheckpointIsAlreadyFaulted_ReportsPermanentFailureWithoutStartingDialog()
    {
        var scenario = CreateScenario(
            telemetryStartException: new EvaluationTelemetryDeliveryRejectedException(422));

        var result = await scenario.Runner.RunAsync(scenario.Context, CancellationToken.None);

        Assert.True(result.WasAbandoned);
        scenario.TerminalSpeech.Verify(
            speech => speech.SpeakSafeAsync(
                FormDialogFlowPromptTextResolver.GetEvaluationPermanentFailurePrompt("de"),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                CancellationToken.None),
            Times.Once);
        scenario.Telemetry.Verify(
            telemetry => telemetry.EndSessionAsync(
                It.IsAny<EvaluationSession>(),
                It.IsAny<EvaluationOutcome>(),
                It.IsAny<EvaluationTaskCompletion>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        scenario.SurfaceController.Verify(controller => controller.BeginSession(), Times.Never);
        scenario.Earcon.Verify(
            earcon => earcon.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_RestoredAcceptedTerminalSkipsTaskReplayAndPublishesOnlyAfterOriginalReceipt()
    {
        var acceptanceSource = new TaskCompletionSource<EvaluationSessionAcceptance>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var restoredTerminal = new EvaluationSessionRestoredTerminal(
            acceptanceSource.Task,
            ParticipantTaskTerminationClassification.Completed);
        var scenario = CreateScenario(restoredTerminal: restoredTerminal);

        var runTask = scenario.Runner.RunAsync(scenario.Context, CancellationToken.None);

        Assert.False(runTask.IsCompleted);
        scenario.TerminalSpeech.Verify(
            speech => speech.SpeakSafeAsync(
                FormDialogFlowPromptTextResolver.GetFormSubmittedPrompt("de"),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                CancellationToken.None),
            Times.Never);

        var acceptance = CreateAcceptance();
        acceptanceSource.SetResult(acceptance);
        var result = await runTask;

        Assert.True(result.WasSubmitted);
        scenario.Telemetry.Verify(
            telemetry => telemetry.CommitSessionAsync(acceptance, CancellationToken.None),
            Times.Once);
        scenario.Telemetry.Verify(
            telemetry => telemetry.EndSessionAsync(
                It.IsAny<EvaluationSession>(),
                It.IsAny<EvaluationOutcome>(),
                It.IsAny<EvaluationTaskCompletion>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        scenario.SurfaceController.Verify(controller => controller.BeginSession(), Times.Never);
        scenario.Earcon.Verify(
            earcon => earcon.PlayAsync(EarconId.FlowDone, CancellationToken.None),
            Times.Once);
        scenario.TerminalSpeech.Verify(
            speech => speech.SpeakSafeAsync(
                FormDialogFlowPromptTextResolver.GetFormSubmittedPrompt("de"),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_RestoredAbandonedRunWaitsForCommitAndAsksForAnExplicitRestartWithoutCompletionCue()
    {
        var acceptanceSource = new TaskCompletionSource<EvaluationSessionAcceptance>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var restoredTerminal = new EvaluationSessionRestoredTerminal(
            acceptanceSource.Task,
            ParticipantTaskTerminationClassification.SessionAbandoned);
        var scenario = CreateScenario(restoredTerminal: restoredTerminal);

        var runTask = scenario.Runner.RunAsync(scenario.Context, CancellationToken.None);

        Assert.False(runTask.IsCompleted);
        scenario.SurfaceController.Verify(controller => controller.BeginSession(), Times.Never);

        var acceptance = CreateAcceptance();
        acceptanceSource.SetResult(acceptance);
        var result = await runTask;

        Assert.True(result.WasAbandoned);
        scenario.Telemetry.Verify(
            telemetry => telemetry.CommitSessionAsync(acceptance, CancellationToken.None),
            Times.Once);
        scenario.Telemetry.Verify(
            telemetry => telemetry.EndSessionAsync(
                It.IsAny<EvaluationSession>(),
                It.IsAny<EvaluationOutcome>(),
                It.IsAny<EvaluationTaskCompletion>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        scenario.SurfaceController.Verify(controller => controller.BeginSession(), Times.Never);
        scenario.Earcon.Verify(
            earcon => earcon.PlayAsync(EarconId.FlowDone, It.IsAny<CancellationToken>()),
            Times.Never);
        scenario.TerminalSpeech.Verify(
            speech => speech.SpeakSafeAsync(
                FormDialogFlowPromptTextResolver.GetEvaluationRestoredAbandonedPrompt("de"),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                CancellationToken.None),
            Times.Once);
    }

    [Theory]
    [InlineData(ParticipantTaskTerminationClassification.ParticipantCancelled)]
    [InlineData(ParticipantTaskTerminationClassification.TechnicalFailure)]
    public async Task RunAsync_RestoredParticipantTerminalSpeaksExactCommittedOutcomeWithoutCompletionCue(
        ParticipantTaskTerminationClassification terminationClassification)
    {
        var acceptanceSource = new TaskCompletionSource<EvaluationSessionAcceptance>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var localCommit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var commitObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var restoredTerminal = new EvaluationSessionRestoredTerminal(
            acceptanceSource.Task,
            terminationClassification);
        var scenario = CreateScenario(
            restoredTerminal: restoredTerminal,
            telemetryCommitTask: localCommit.Task,
            commitObserver: () => commitObserved.TrySetResult());

        var runTask = scenario.Runner.RunAsync(scenario.Context, CancellationToken.None);

        Assert.False(runTask.IsCompleted);
        Assert.Empty(scenario.TerminalSpeech.Invocations);
        Assert.Empty(scenario.Earcon.Invocations);

        acceptanceSource.SetResult(CreateAcceptance());
        await commitObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(runTask.IsCompleted);
        Assert.Empty(scenario.TerminalSpeech.Invocations);
        Assert.Empty(scenario.Earcon.Invocations);

        localCommit.SetResult();
        var result = await runTask;

        Assert.Equal(
            terminationClassification == ParticipantTaskTerminationClassification.ParticipantCancelled,
            result.WasCancelled);
        Assert.Equal(
            terminationClassification == ParticipantTaskTerminationClassification.TechnicalFailure,
            result.WasAbandoned);
        var speech = Assert.Single(scenario.TerminalSpeech.Invocations);
        var expectedPrompt = terminationClassification == ParticipantTaskTerminationClassification.ParticipantCancelled
            ? FormDialogFlowPromptTextResolver.GetEvaluationTaskCancelledPrompt("de")
            : FormDialogFlowPromptTextResolver.GetEvaluationTechnicalFailurePrompt("de");
        Assert.Equal(expectedPrompt, speech.Arguments[0]);
        Assert.Empty(scenario.Earcon.Invocations);
    }

    [Theory]
    [InlineData(ParticipantTaskTerminationClassification.Completed)]
    [InlineData(ParticipantTaskTerminationClassification.ParticipantCancelled)]
    [InlineData(ParticipantTaskTerminationClassification.TechnicalFailure)]
    public async Task RunAsync_WhenOptionalTerminalSpeechFails_PreservesAcceptedOutcomeAndPublication(
        ParticipantTaskTerminationClassification classification)
    {
        EvaluationTaskCompletion? completion = null;
        var committed = false;
        bool? committedWhenSpeechStarted = null;
        var scenario = CreateScenario(
            promptException: new SpeechErrorException(SpeechErrorCode.ProviderFailure, "synthetic first audio timeout"),
            successfulSubmission: classification == ParticipantTaskTerminationClassification.Completed,
            cancelled: classification == ParticipantTaskTerminationClassification.ParticipantCancelled,
            completionObserver: value => completion = value,
            commitObserver: () => committed = true);
        scenario.TerminalSpeech.Setup(s => s.SpeakSafeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), CancellationToken.None))
            .Callback(() => committedWhenSpeechStarted = committed)
            .ThrowsAsync(new SpeechErrorException(SpeechErrorCode.ProviderFailure, "synthetic terminal audio timeout"));

        var result = await scenario.Runner.RunAsync(scenario.Context, CancellationToken.None);

        Assert.True(committedWhenSpeechStarted);
        Assert.NotNull(completion);
        Assert.Equal(classification, completion.TerminationClassification);
        Assert.Equal(classification == ParticipantTaskTerminationClassification.Completed, result.WasSubmitted);
        Assert.Equal(classification == ParticipantTaskTerminationClassification.ParticipantCancelled, result.WasCancelled);
        Assert.Equal(classification == ParticipantTaskTerminationClassification.TechnicalFailure, result.WasAbandoned);
        if (classification != ParticipantTaskTerminationClassification.ParticipantCancelled)
            Assert.Equal("Max", result.ValuesByVariableId["name"]);
        scenario.Telemetry.Verify(t => t.EndSessionAsync(scenario.Session, It.IsAny<EvaluationOutcome>(),
            It.IsAny<EvaluationTaskCompletion>(), CancellationToken.None), Times.Once);
        scenario.Telemetry.Verify(t => t.CommitSessionAsync(It.IsAny<EvaluationSessionAcceptance>(), CancellationToken.None), Times.Once);
        scenario.SurfaceController.Verify(c => c.TryPublishTerminal(It.IsAny<FormSurfacePublicationSession>(),
            It.IsAny<SpeechAssistantFormSurfaceView>()), Times.Once);
        Assert.Single(scenario.TerminalSpeech.Invocations);
    }

    [Theory]
    [InlineData(ParticipantTaskTerminationClassification.Completed)]
    [InlineData(ParticipantTaskTerminationClassification.ParticipantCancelled)]
    [InlineData(ParticipantTaskTerminationClassification.TechnicalFailure)]
    [InlineData(ParticipantTaskTerminationClassification.SessionAbandoned)]
    public async Task RunAsync_WhenRestoredTerminalSpeechFails_ReturnsOriginalOutcomeWithoutReplay(
        ParticipantTaskTerminationClassification classification)
    {
        var acceptance = CreateAcceptance();
        var scenario = CreateScenario(restoredTerminal: new EvaluationSessionRestoredTerminal(
            Task.FromResult(acceptance), classification));
        scenario.TerminalSpeech.Setup(s => s.SpeakSafeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), CancellationToken.None))
            .ThrowsAsync(new SpeechErrorException(SpeechErrorCode.ProviderFailure, "synthetic terminal audio timeout"));

        var result = await scenario.Runner.RunAsync(scenario.Context, CancellationToken.None);

        Assert.Equal(classification == ParticipantTaskTerminationClassification.Completed, result.WasSubmitted);
        Assert.Equal(classification == ParticipantTaskTerminationClassification.ParticipantCancelled, result.WasCancelled);
        Assert.Equal(classification is ParticipantTaskTerminationClassification.TechnicalFailure or
            ParticipantTaskTerminationClassification.SessionAbandoned, result.WasAbandoned);
        Assert.Equal(scenario.Context.ValuesByVariableId, result.ValuesByVariableId);
        scenario.Telemetry.Verify(t => t.CommitSessionAsync(acceptance, CancellationToken.None), Times.Once);
        scenario.Telemetry.Verify(t => t.EndSessionAsync(It.IsAny<EvaluationSession>(), It.IsAny<EvaluationOutcome>(),
            It.IsAny<EvaluationTaskCompletion>(), It.IsAny<CancellationToken>()), Times.Never);
        scenario.SurfaceController.Verify(c => c.BeginSession(), Times.Never);
        Assert.Single(scenario.TerminalSpeech.Invocations);
    }

    [Fact]
    public async Task RunAsync_WhenLocalCommitFails_StillFailsBeforeOptionalTerminalSpeechOrPublication()
    {
        var scenario = CreateScenario(successfulSubmission: true,
            telemetryCommitTask: Task.FromException(new IOException("synthetic local commit failure")));

        await Assert.ThrowsAsync<IOException>(() => scenario.Runner.RunAsync(scenario.Context, CancellationToken.None));

        Assert.Empty(scenario.TerminalSpeech.Invocations);
        scenario.SurfaceController.Verify(c => c.TryPublishTerminal(It.IsAny<FormSurfacePublicationSession>(),
            It.IsAny<SpeechAssistantFormSurfaceView>()), Times.Never);
    }

    private static (
        FormVoiceCommandSessionRunner Runner,
        FormVoiceCommandContext Context,
        Mock<IEvaluationSessionTelemetry> Telemetry,
        EvaluationSession Session,
        Mock<ISpeechCaptureFeedback> SpeechCaptureFeedback,
        Mock<ISpeechAssistantFormSurfaceController> SurfaceController,
        Mock<IFormVoiceCommandSpeechService> TerminalSpeech,
        Mock<IEarconPlayer> Earcon) CreateScenario(
        Exception? promptException = null,
        Exception? runtimeCreateException = null,
        Task<EvaluationSessionAcceptance?>? telemetryEndTask = null,
        Task? telemetryCommitTask = null,
        Action<EvaluationTaskCompletion>? completionObserver = null,
        Action? commitObserver = null,
        bool successfulSubmission = false,
        bool cancelled = false,
        EvaluationSessionRestoredTerminal? restoredTerminal = null,
        Exception? telemetryStartException = null)
    {
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        if (successfulSubmission)
        {
            context = context with
            {
                Selection = context.Selection with
                {
                    VoiceSettings = new VoiceDialogSettings { AutoAdvanceAfterCommit = false },
                },
            };
        }

        // Der echte Interpreter erhält einen vorbelegten Wert, den der Runner auch beim Pausieren im Ergebnis zurückgeben soll.
        var initialValues = ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!cancelled)
        {
            initialValues = initialValues.Add("name", "Max");
        }

        var runtime = new FormInterpreterRuntimeOwner(contract, "de", initialValues);

        var runtimeFactory = new Mock<IFormDialogRuntimeFactory>();
        if (runtimeCreateException is not null)
        {
            runtimeFactory
                .Setup(f => f.Create(
                    It.IsAny<IFormDialogRuntimeContext>(),
                    It.IsAny<string>(),
                    It.IsAny<IFormRuleLoopGuardFeedback>()))
                .Throws(runtimeCreateException);
        }
        else
        {
            runtimeFactory
                .Setup(f => f.Create(
                    It.IsAny<IFormDialogRuntimeContext>(),
                    It.IsAny<string>(),
                    It.IsAny<IFormRuleLoopGuardFeedback>()))
                .Returns(runtime);
        }

        var telemetry = new Mock<IEvaluationSessionTelemetry>();
        var session = new EvaluationSession("session-1", EvaluationChannel.Voice, "app", "current")
        {
            RestoredTerminal = restoredTerminal,
        };
        var startSession = telemetry
            .Setup(t => t.StartSessionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<EvaluationChannel>(),
                It.IsAny<CancellationToken>()));
        if (telemetryStartException is null)
        {
            startSession.ReturnsAsync(session);
        }
        else
        {
            startSession.ThrowsAsync(telemetryStartException);
        }
        telemetry
            .Setup(t => t.EndSessionAsync(
                It.IsAny<EvaluationSession>(),
                It.IsAny<EvaluationOutcome>(),
                It.IsAny<EvaluationTaskCompletion>(),
                It.IsAny<CancellationToken>()))
            .Callback<EvaluationSession, EvaluationOutcome, EvaluationTaskCompletion, CancellationToken>(
                (_, _, completion, _) => completionObserver?.Invoke(completion))
            .Returns(telemetryEndTask ?? Task.FromResult<EvaluationSessionAcceptance?>(CreateAcceptance()));
        telemetry
            .Setup(t => t.CommitSessionAsync(
                It.IsAny<EvaluationSessionAcceptance>(),
                It.IsAny<CancellationToken>()))
            .Callback<EvaluationSessionAcceptance, CancellationToken>((_, _) => commitObserver?.Invoke())
            .Returns(telemetryCommitTask ?? Task.CompletedTask);

        var formTelemetry = new Mock<IEvaluationFormTelemetry>();

        var surfaceDefinitionBuilder = new Mock<IFormSurfaceDefinitionBuilder>();
        surfaceDefinitionBuilder
            .Setup(b => b.BuildFormSurfaceDefinition(
                It.IsAny<FormContract>(),
                It.IsAny<string>(),
                It.IsAny<PolitenessLevel>()))
            .Returns(new FormSurfaceDefinition(
                "main",
                new Dictionary<string, FormSurfaceFieldGroupInfo>(StringComparer.OrdinalIgnoreCase),
                []));

        var surfaceController = new Mock<ISpeechAssistantFormSurfaceController>();
        surfaceController
            .Setup(controller => controller.BeginSession())
            .Returns(new FormSurfacePublicationSession(1, Guid.NewGuid()));
        surfaceController
            .Setup(controller => controller.TryPublishActive(
                It.IsAny<FormSurfacePublicationSession>(),
                It.IsAny<SpeechAssistantFormSurfaceView>()))
            .Returns(true);
        surfaceController
            .Setup(controller => controller.TryPublishTerminal(
                It.IsAny<FormSurfacePublicationSession>(),
                It.IsAny<SpeechAssistantFormSurfaceView>()))
            .Returns(true);
        var surfacePublisher = new FormVoiceCommandSessionSurfacePublisher(
            surfaceDefinitionBuilder.Object,
            new SpeechAssistantFormSurfacePublication(
                new FormSurfaceSnapshotAssembler(),
                surfaceController.Object));

        // Im Standardfall löst die Ansage übernommener Werte eine Pause vor der Feldbearbeitung aus.
        // Die Erfolgs- und Abbruchfälle lassen diese Ansage passieren.
        var promptService = new Mock<IFormVoiceCommandPromptService>();
        var sharedAnnouncement = promptService.Setup(p => p.SpeakSharedVariableAnnouncementAsync(
            It.IsAny<ApplicationDescriptor>(),
            It.IsAny<FormDescriptor>(),
            It.IsAny<FormContract>(),
            It.IsAny<ImmutableArray<(string, string?)>>(),
            It.IsAny<string>(),
            It.IsAny<VoiceDialogSettings>(),
            It.IsAny<CancellationToken>()));
        if (successfulSubmission || cancelled)
        {
            sharedAnnouncement.Returns(Task.CompletedTask);
            promptService
                .Setup(p => p.SpeakFormOrientationAsync(
                    It.IsAny<ApplicationDescriptor>(),
                    It.IsAny<FormDescriptor>(),
                    It.IsAny<FormContract>(),
                    It.IsAny<IFormDialogRuntime>(),
                    It.IsAny<string>(),
                    It.IsAny<VoiceDialogSettings>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }
        else
        {
            sharedAnnouncement.ThrowsAsync(promptException ?? new VoiceInputPausedException());
        }

        var resultFactory = new FormVoiceCommandSessionResultFactory();
        var speechCaptureFeedback = new Mock<ISpeechCaptureFeedback>(MockBehavior.Strict);

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>();
        if (successfulSubmission)
        {
            submissionService
                .Setup(service => service.TrySubmitAsync(
                    It.IsAny<IFormDialogRuntime>(),
                    It.IsAny<FormContract>(),
                    It.IsAny<EvaluationSession>(),
                    It.IsAny<string>(),
                    It.IsAny<VoiceDialogSettings>(),
                    It.IsAny<IFormVoiceCommandDialogStateTracker>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(FormSubmissionResult.Submitted);
        }

        var fieldCoordinator = new FormVoiceCommandFieldCoordinator(
            promptService.Object,
            submissionService.Object,
            cancelled
                ? new CancelledFieldInteractionHandler()
                : successfulSubmission
                    ? new SubmittedFieldInteractionHandler()
                    : new ThrowingFieldInteractionHandler(),
            surfacePublisher,
            resultFactory,
            new FormClosingReviewTurnFlow(
                Mock.Of<IFormVoiceCommandSpeechService>(),
                new FormVoiceCommandGlobalCommandExecutionService(
                    Mock.Of<IEvaluationCommandTelemetry>(),
                    Mock.Of<IFormVoiceCommandGlobalCommandRouter>())));

        var terminalSpeech = new Mock<IFormVoiceCommandSpeechService>();
        terminalSpeech
            .Setup(speech => speech.SpeakSafeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var earcon = new Mock<IEarconPlayer>();
        earcon
            .Setup(player => player.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        var terminalFeedbackOwner = new FormVoiceCommandTerminalFeedbackOwner(
            terminalSpeech.Object,
            earcon.Object,
            surfacePublisher,
            new FormVoiceCommandTerminalWorkLifetime());

        var runner = new FormVoiceCommandSessionRunner(
            telemetry.Object,
            formTelemetry.Object,
            speechCaptureFeedback.Object,
            runtimeFactory.Object,
            surfacePublisher,
            terminalFeedbackOwner,
            fieldCoordinator,
            resultFactory,
            NullLogger<FormVoiceCommandSessionRunner>.Instance);

        return (runner, context, telemetry, session, speechCaptureFeedback, surfaceController, terminalSpeech, earcon);
    }

    private static EvaluationSessionAcceptance CreateAcceptance()
        => new("session-1", Guid.NewGuid().ToString("D"), new string('a', 64));

    private static FormContract CreateMinimalContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "name",
                    ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement
                }
            ],
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

    private static FormVoiceCommandContext CreateContext(FormContract contract)
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            Description = new LocalizedText { ["de"] = "Teil 1" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "App" },
            Description = new LocalizedText { ["de"] = "App" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = [form]
        };

        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: []),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: ImmutableArray.Create<(string VariableId, string? SourceFormId)>(("name", null))));
    }

}
