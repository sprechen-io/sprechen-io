using Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SelectionDialogTurnFeedbackRuleTests
{
    private static readonly SelectionDialogPrompt ReListenPrompt = SelectionDialogPrompt.FromText("re-listen");

    [Fact]
    public async Task ResolveSpokenPromptAsync_WhenListeningStepHasNoPrompt_SubstitutesReListenPrompt()
    {
        var rule = CreateRule();
        var turnSnapshot = CreateTurnSnapshot();
        var step = new SelectionDialogIntentStep<string>(
            SelectionDialogPhase.Listening,
            SelectionDialogCandidateTransition<string>.Preserve(),
            spokenPrompt: null);

        var resolved = await rule.ResolveSpokenPromptAsync(step, turnSnapshot, CancellationToken.None);

        Assert.Same(ReListenPrompt, resolved);
    }

    [Fact]
    public async Task ResolveSpokenPromptAsync_WhenListeningStepHasEmptyPrompt_SubstitutesReListenPrompt()
    {
        var rule = CreateRule();
        var turnSnapshot = CreateTurnSnapshot();
        var step = new SelectionDialogIntentStep<string>(
            SelectionDialogPhase.Listening,
            SelectionDialogCandidateTransition<string>.Preserve(),
            SelectionDialogPrompt.FromText(string.Empty));

        var resolved = await rule.ResolveSpokenPromptAsync(step, turnSnapshot, CancellationToken.None);

        Assert.Same(ReListenPrompt, resolved);
    }

    [Fact]
    public async Task ResolveSpokenPromptAsync_WhenConfirmingStepHasNoPrompt_SubstitutesReListenPrompt()
    {
        var rule = CreateRule();
        var turnSnapshot = CreateTurnSnapshot();
        var step = new SelectionDialogIntentStep<string>(
            SelectionDialogPhase.Confirming,
            SelectionDialogCandidateTransition<string>.SelectUnique(
                turnSnapshot.VisibleCandidates[0],
                SelectionCandidateResolutionKind.ExactName),
            spokenPrompt: null);

        var resolved = await rule.ResolveSpokenPromptAsync(step, turnSnapshot, CancellationToken.None);

        Assert.Same(ReListenPrompt, resolved);
    }

    [Fact]
    public async Task ResolveSpokenPromptAsync_WhenListeningStepAlreadySpeaks_KeepsItsPrompt()
    {
        var rule = CreateRule();
        var turnSnapshot = CreateTurnSnapshot();
        var prompt = SelectionDialogPrompt.FromText("not understood");
        var step = new SelectionDialogIntentStep<string>(
            SelectionDialogPhase.Listening,
            SelectionDialogCandidateTransition<string>.Preserve(),
            prompt);

        var resolved = await rule.ResolveSpokenPromptAsync(step, turnSnapshot, CancellationToken.None);

        Assert.Same(prompt, resolved);
    }

    [Theory]
    [InlineData(SelectionDialogPhase.Completed)]
    [InlineData(SelectionDialogPhase.Launch)]
    [InlineData(SelectionDialogPhase.Announcing)]
    public async Task ResolveSpokenPromptAsync_WhenStepIsTerminalOrSelfAnnouncing_DoesNotSubstitute(
        SelectionDialogPhase nextPhase)
    {
        var rule = CreateRule();
        var turnSnapshot = CreateTurnSnapshot();
        var candidateTransition = nextPhase switch
        {
            SelectionDialogPhase.Completed => SelectionDialogCandidateTransition<string>.ClearTerminal(),
            SelectionDialogPhase.Launch => SelectionDialogCandidateTransition<string>.SelectUnique(
                turnSnapshot.VisibleCandidates[0],
                SelectionCandidateResolutionKind.ExactName),
            SelectionDialogPhase.Announcing => SelectionDialogCandidateTransition<string>.Preserve(),
            _ => throw new InvalidOperationException($"Unexpected test phase '{nextPhase}'.")
        };
        var step = new SelectionDialogIntentStep<string>(nextPhase, candidateTransition, spokenPrompt: null);

        var resolved = await rule.ResolveSpokenPromptAsync(step, turnSnapshot, CancellationToken.None);

        Assert.Null(resolved);
    }

    private static SelectionDialogTurnFeedbackRule<string> CreateRule()
        => new((_, _) => Task.FromResult(ReListenPrompt));

    private static SelectionDialogTurnSnapshot<string> CreateTurnSnapshot()
    {
        var revision = SelectionRevision.Initial;
        var candidate = SelectionCandidate<string>.Create(
            "item",
            identifier: "item",
            displayName: "Item",
            stableNumber: 1,
            revision);
        var candidates = SelectionCandidateSnapshot<string>.Create(revision, [candidate]);

        return new SelectionDialogTurnSnapshot<string>(
            candidates,
            candidates,
            activeAmbiguity: null,
            selectedCandidate: null,
            selectedResolutionKind: null);
    }
}
