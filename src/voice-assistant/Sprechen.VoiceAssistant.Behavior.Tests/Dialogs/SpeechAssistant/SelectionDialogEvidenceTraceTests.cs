namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

public sealed class SelectionDialogEvidenceTraceTests
{
    [Fact]
    public async Task CandidateTransitionAppliedAsync_WhenActiveAmbiguityIsResolved_EmitsInitialSetThenChoiceOnly()
    {
        var trace = new RecordingSelectionSpeechAssistantTrace();
        var evidence = CreateEvidence(trace);
        var candidates = CreateCandidates();

        await evidence.CandidateTransitionAppliedAsync(
            SelectionDialogCandidateTransition<string>.RetainAmbiguity(candidates),
            hadActiveAmbiguity: false,
            CancellationToken.None);
        await evidence.CandidateTransitionAppliedAsync(
            SelectionDialogCandidateTransition<string>.SelectUnique(
                candidates[1],
                SelectionCandidateResolutionKind.ExactName),
            hadActiveAmbiguity: true,
            CancellationToken.None);

        Assert.Equal(
            [
                "catalog-selection event=candidates_matched candidateIds=[alpha,beta]",
                "catalog-selection event=candidate_chosen candidateId=beta"
            ],
            trace.Entries.Select(static entry => entry.Message).ToArray());
    }

    [Fact]
    public async Task PromptScheduling_UsesClosedStablePromptIdentifiers()
    {
        var trace = new RecordingSelectionSpeechAssistantTrace();
        var evidence = CreateEvidence(trace);
        var candidate = CreateCandidates()[0];
        var prompt = SelectionDialogPrompt.FromText("modeled prompt");

        await evidence.AnnouncementPromptScheduledAsync(prompt, CancellationToken.None);
        await evidence.AppliedPhasePromptScheduledAsync(
            SelectionDialogPhase.Listening,
            completionReason: null,
            prompt,
            CancellationToken.None);
        await evidence.ConfirmationPromptScheduledAsync(
            SelectionConfirmationPromptKind.InitialConfirmation,
            SelectionConfirmationCaptureStage.Initial,
            candidate,
            prompt,
            CancellationToken.None);
        await evidence.ConfirmationPromptScheduledAsync(
            SelectionConfirmationPromptKind.ExplicitAuthorization,
            SelectionConfirmationCaptureStage.ExplicitAuthorization,
            candidate,
            prompt,
            CancellationToken.None);
        await evidence.ConfirmationPromptScheduledAsync(
            SelectionConfirmationPromptKind.VerificationUnavailable,
            SelectionConfirmationCaptureStage.ConfidenceFaultRetry,
            candidate,
            prompt,
            CancellationToken.None);
        await evidence.ConfirmationPromptScheduledAsync(
            SelectionConfirmationPromptKind.NoInputReminder,
            SelectionConfirmationCaptureStage.Initial,
            candidate,
            prompt,
            CancellationToken.None);
        await evidence.LaunchPromptScheduledAsync(candidate, prompt, CancellationToken.None);
        await evidence.ConfirmationPromptScheduledAsync(
            SelectionConfirmationPromptKind.Cancellation,
            SelectionConfirmationCaptureStage.Initial,
            candidate,
            prompt,
            CancellationToken.None);

        Assert.Equal(
            [
                "selection.announce",
                "selection.listening",
                "selection.confirmation.initial",
                "selection.confirmation.explicitAuthorization",
                "selection.confirmation.confidenceFaultRetry",
                "selection.confirmation.noInput",
                "selection.launch",
                "selection.cancelled"
            ],
            trace.Entries
                .Select(static entry => entry.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Select(static parts => parts.Single(part => part.StartsWith("promptId=", StringComparison.Ordinal))[9..])
                .ToArray());
    }

    [Fact]
    public async Task PromptScheduling_EmitsLowercaseUtf8Sha256WithoutPromptText()
    {
        const string secretPrompt = "Vertraulich-Ä-42";
        const string expectedSha256 = "d1f952f7f7df5aed4cf2e72c0c7408302cb9c43e1f12264635b5cb252545df84";
        var trace = new RecordingSelectionSpeechAssistantTrace();
        var evidence = CreateEvidence(trace);

        await evidence.AppliedPhasePromptScheduledAsync(
            SelectionDialogPhase.Listening,
            completionReason: null,
            SelectionDialogPrompt.FromText(secretPrompt),
            CancellationToken.None);

        var entry = Assert.Single(trace.Entries);
        Assert.Equal(
            $"catalog-selection event=prompt_scheduled promptId=selection.listening promptSha256={expectedSha256}",
            entry.Message);
        Assert.DoesNotContain(secretPrompt, entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(" text=", entry.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(SelectionDialogCompletionReason.UserCancelled)]
    [InlineData(SelectionDialogCompletionReason.RestartRequested)]
    [InlineData(SelectionDialogCompletionReason.CaptureFaultBudgetExhausted)]
    public async Task AppliedPhasePromptScheduling_WhenCompletionWasApplied_UsesCancelledIdentifier(
        SelectionDialogCompletionReason completionReason)
    {
        var trace = new RecordingSelectionSpeechAssistantTrace();
        var evidence = CreateEvidence(trace);

        await evidence.AppliedPhasePromptScheduledAsync(
            SelectionDialogPhase.Completed,
            completionReason,
            SelectionDialogPrompt.FromText("terminal prompt"),
            CancellationToken.None);

        Assert.Contains(
            "promptId=selection.cancelled",
            Assert.Single(trace.Entries).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task AppliedPhasePromptScheduling_WhenCompletedReasonIsMissing_FailsFast()
    {
        var evidence = CreateEvidence(new RecordingSelectionSpeechAssistantTrace());

        await Assert.ThrowsAsync<SelectionCandidateStateException>(async () =>
            await evidence.AppliedPhasePromptScheduledAsync(
                SelectionDialogPhase.Completed,
                completionReason: null,
                SelectionDialogPrompt.FromText("terminal prompt"),
                CancellationToken.None));
    }

    [Theory]
    [InlineData(SelectionDialogPhase.Announcing)]
    [InlineData(SelectionDialogPhase.Listening)]
    [InlineData(SelectionDialogPhase.Confirming)]
    [InlineData(SelectionDialogPhase.Launch)]
    public async Task AppliedPhasePromptScheduling_WhenNonTerminalCarriesReason_FailsFast(
        SelectionDialogPhase phase)
    {
        var evidence = CreateEvidence(new RecordingSelectionSpeechAssistantTrace());

        await Assert.ThrowsAsync<SelectionCandidateStateException>(async () =>
            await evidence.AppliedPhasePromptScheduledAsync(
                phase,
                SelectionDialogCompletionReason.UserCancelled,
                SelectionDialogPrompt.FromText("invalid prompt"),
                CancellationToken.None));
    }

    [Fact]
    public async Task AppliedPhasePromptScheduling_WhenPhaseIsUnknown_FailsFast()
    {
        var evidence = CreateEvidence(new RecordingSelectionSpeechAssistantTrace());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await evidence.AppliedPhasePromptScheduledAsync(
                (SelectionDialogPhase)int.MaxValue,
                completionReason: null,
                SelectionDialogPrompt.FromText("invalid prompt"),
                CancellationToken.None));
    }

    [Fact]
    public async Task AppliedPhasePromptScheduling_WhenCompletionReasonIsUnknown_FailsFast()
    {
        var evidence = CreateEvidence(new RecordingSelectionSpeechAssistantTrace());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await evidence.AppliedPhasePromptScheduledAsync(
                SelectionDialogPhase.Completed,
                (SelectionDialogCompletionReason)int.MaxValue,
                SelectionDialogPrompt.FromText("invalid prompt"),
                CancellationToken.None));
    }

    private static SelectionDialogEvidenceTrace<string> CreateEvidence(ISpeechAssistantTrace trace)
        => new(
            new SpeechAssistantTraceContext(
                trace,
                "evidence-test",
                SpeechAssistantTraceScope.SelectionDialogRuntime));

    private static SelectionCandidateSnapshot<string> CreateCandidates()
    {
        var revision = SelectionRevision.Initial;
        return SelectionCandidateSnapshot<string>.Create(
            revision,
            [
                SelectionCandidate<string>.Create("alpha", "alpha", "Alpha", 1, revision),
                SelectionCandidate<string>.Create("beta", "beta", "Beta", 2, revision)
            ]);
    }
}
