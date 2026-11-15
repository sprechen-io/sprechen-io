using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Projiziert angewandte Auswahlübergänge und eingeplante Ansagen in eine anwendungsneutrale
/// Diagnose. Die Aufzeichnung entscheidet nicht über den Dialog.
/// </summary>
internal sealed class SelectionDialogEvidenceTrace<TSelection>
    where TSelection : class
{
    private const string EventPrefix = "catalog-selection event=";

    private readonly SpeechAssistantTraceContext _traceContext;
    private readonly SelectionDialogEvidencePromptIdResolver _promptIds = new();

    /// <summary>
    /// Bindet die Diagnose an den Kontext eines Auswahllaufs, damit dessen Ereignisse zugeordnet
    /// werden können.
    /// </summary>
    public SelectionDialogEvidenceTrace(SpeechAssistantTraceContext traceContext)
    {
        _traceContext = traceContext ?? throw new ArgumentNullException(nameof(traceContext));
    }

    internal async ValueTask CandidateTransitionAppliedAsync(
        SelectionDialogCandidateTransition<TSelection> transition,
        bool hadActiveAmbiguity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transition);

        switch (transition.Kind)
        {
            case SelectionDialogCandidateTransitionKind.VisibleReplacement:
            case SelectionDialogCandidateTransitionKind.AmbiguousVisibleReplacement:
                await CandidatesMatchedAsync(
                        transition.Snapshot?.Candidates
                        ?? throw new SelectionCandidateStateException(
                            "A candidate-set transition requires its applied snapshot."),
                        cancellationToken)
                    .ConfigureAwait(false);
                return;

            case SelectionDialogCandidateTransitionKind.AmbiguityRetention:
                if (!hadActiveAmbiguity)
                {
                    await CandidatesMatchedAsync(
                            transition.Snapshot?.Candidates
                            ?? throw new SelectionCandidateStateException(
                                "A candidate-set transition requires its applied snapshot."),
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                return;

            case SelectionDialogCandidateTransitionKind.UniqueSelection:
                var candidate = transition.Candidate
                    ?? throw new SelectionCandidateStateException(
                        "A unique-selection transition requires its applied candidate.");
                if (!hadActiveAmbiguity)
                {
                    await CandidatesMatchedAsync([candidate], cancellationToken).ConfigureAwait(false);
                }

                await WriteAsync(
                        $"{EventPrefix}candidate_chosen candidateId={FormatIdentifier(candidate.Identifier)}",
                        cancellationToken)
                    .ConfigureAwait(false);
                return;

            default:
                return;
        }
    }

    internal ValueTask ConfirmationDecisionAppliedAsync(
        SelectionConfirmationTransitionDecision<TSelection> decision,
        SelectionConfirmationStateSnapshot<TSelection> priorState,
        SelectionConfirmationDecisionInputs? decisionInputs,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(priorState);

        var candidate = decision.ExpectedCandidate;
        if (candidate is null)
        {
            return ValueTask.CompletedTask;
        }

        var candidateId = FormatIdentifier(candidate.Identifier);
        var priorStage = priorState.Stage?.ToString()
            ?? throw new SelectionCandidateStateException(
                "An applied confirmation decision requires its prior capture stage.");
        var inputs = FormatDecisionInputs(decisionInputs);
        return decision.Kind switch
        {
            SelectionConfirmationTransitionKind.EnterExplicitAuthorization or
            SelectionConfirmationTransitionKind.EnterConfidenceFaultRetry => WriteAsync(
                $"{EventPrefix}confirmation_repair_requested candidateId={candidateId} stage={decision.NextStage}{inputs}",
                cancellationToken),
            SelectionConfirmationTransitionKind.Launch => WriteAsync(
                $"{EventPrefix}confirmation_accepted candidateId={candidateId} stage={priorStage}{inputs}",
                cancellationToken),
            SelectionConfirmationTransitionKind.Reselect => WriteAsync(
                $"{EventPrefix}confirmation_rejected candidateId={candidateId} stage={priorStage}{inputs}",
                cancellationToken),
            SelectionConfirmationTransitionKind.Cancellation => WriteAsync(
                $"{EventPrefix}selection_cancel_requested candidateId={candidateId} stage={priorStage}",
                cancellationToken),
            _ => ValueTask.CompletedTask,
        };
    }

    private static string FormatDecisionInputs(SelectionConfirmationDecisionInputs? decisionInputs)
    {
        if (decisionInputs is null)
        {
            return string.Empty;
        }

        var intent = decisionInputs.Intent?.ToString() ?? "none";
        var percent = FormatOptionalPercent(decisionInputs.SttConfidencePercent);
        return $" confidence={percent} intent={intent} competingEvidence={decisionInputs.HasCompetingIntentEvidence}";
    }

    private static string FormatOptionalPercent(int? percent)
    {
        if (percent is null)
        {
            return "none";
        }

        return percent.Value.ToString(CultureInfo.InvariantCulture);
    }

    internal ValueTask AnnouncementPromptScheduledAsync(
        SelectionDialogPrompt prompt,
        CancellationToken cancellationToken)
        => PromptScheduledAsync(_promptIds.Announcement, prompt, candidate: null, stage: null, cancellationToken);

    internal ValueTask EmptyPromptScheduledAsync(
        SelectionDialogPrompt prompt,
        CancellationToken cancellationToken)
        => PromptScheduledAsync(_promptIds.Cancelled, prompt, candidate: null, stage: null, cancellationToken);

    internal ValueTask NoInputPromptScheduledAsync(
        SelectionDialogPrompt prompt,
        CancellationToken cancellationToken)
        => PromptScheduledAsync(_promptIds.Listening, prompt, candidate: null, stage: null, cancellationToken);

    internal ValueTask AppliedPhasePromptScheduledAsync(
        SelectionDialogPhase appliedPhase,
        SelectionDialogCompletionReason? completionReason,
        SelectionDialogPrompt prompt,
        CancellationToken cancellationToken)
        => PromptScheduledAsync(
            _promptIds.ResolveAppliedPhase(appliedPhase, completionReason),
            prompt,
            candidate: null,
            stage: null,
            cancellationToken);

    internal ValueTask ConfirmationPromptScheduledAsync(
        SelectionConfirmationPromptKind promptKind,
        SelectionConfirmationCaptureStage stage,
        SelectionCandidate<TSelection> candidate,
        SelectionDialogPrompt prompt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return PromptScheduledAsync(
            _promptIds.ResolveConfirmation(promptKind, stage),
            prompt,
            candidate,
            stage,
            cancellationToken);
    }

    /// <summary>
    /// Protokolliert vor dem Wiedergabeaufruf einen Hash des Übergabetexts zur Zuordnung mit weiteren
    /// Ausgaben. Ein Texthash allein gewährleistet weder Anonymität noch eine tatsächliche
    /// Wiedergabe.
    /// </summary>
    internal ValueTask LaunchPromptScheduledAsync(
        SelectionCandidate<TSelection> candidate,
        SelectionDialogPrompt prompt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return PromptScheduledAsync(_promptIds.Launch, prompt, candidate, stage: null, cancellationToken);
    }

    internal ValueTask SelectionLaunchedAsync(
        SelectionCandidate<TSelection> candidate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return WriteAsync(
            $"{EventPrefix}selection_launch candidateId={FormatIdentifier(candidate.Identifier)}",
            cancellationToken);
    }

    internal ValueTask SelectionCancelledAsync(
        SelectionDialogCompletionReason reason,
        CancellationToken cancellationToken)
        => WriteAsync($"{EventPrefix}selection_cancelled reason={reason}", cancellationToken);

    internal async ValueTask SingleCandidateSelectedAsync(
        SelectionCandidate<TSelection> candidate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        await CandidatesMatchedAsync([candidate], cancellationToken).ConfigureAwait(false);
        await WriteAsync(
                $"{EventPrefix}candidate_chosen candidateId={FormatIdentifier(candidate.Identifier)}",
                cancellationToken)
            .ConfigureAwait(false);
    }

    private ValueTask CandidatesMatchedAsync(
        IEnumerable<SelectionCandidate<TSelection>> candidates,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var candidateIds = candidates.Select(static candidate => FormatIdentifier(candidate.Identifier));
        return WriteAsync(
            $"{EventPrefix}candidates_matched candidateIds=[{string.Join(',', candidateIds)}]",
            cancellationToken);
    }

    private ValueTask PromptScheduledAsync(
        string promptId,
        SelectionDialogPrompt prompt,
        SelectionCandidate<TSelection>? candidate,
        SelectionConfirmationCaptureStage? stage,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        ArgumentNullException.ThrowIfNull(prompt);
        if (!prompt.HasContent)
        {
            return ValueTask.CompletedTask;
        }

        var candidatePart = candidate is null
            ? string.Empty
            : $" candidateId={FormatIdentifier(candidate.Identifier)}";
        var stagePart = stage.HasValue ? $" stage={stage.Value}" : string.Empty;
        return WriteAsync(
            $"{EventPrefix}prompt_scheduled promptId={promptId}{candidatePart}{stagePart} promptSha256={ComputePromptSha256(prompt.Text)}",
            cancellationToken);
    }

    private ValueTask WriteAsync(string message, CancellationToken cancellationToken)
        => _traceContext.Trace.WriteIfConfiguredAsync(
            _traceContext.CorrelationId,
            _traceContext.Scope,
            SpeechAssistantTraceLevel.Debug,
            message,
            cancellationToken);

    private static string FormatIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return Uri.EscapeDataString(identifier.Trim());
    }

    private static string ComputePromptSha256(string text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}
