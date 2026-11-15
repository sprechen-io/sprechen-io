namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält den Kandidatenkontext für einen Auswahlaufruf fest und prüft Referenzzugehörigkeit und
/// Revision der aktiven Mehrdeutigkeit sowie des ausgewählten Kandidaten. Die Fachobjekte selbst
/// werden nicht tief unveränderlich gemacht.
/// </summary>
public sealed class SelectionDialogTurnSnapshot<TSelection>
    where TSelection : class
{
    internal SelectionDialogTurnSnapshot(
        SelectionCandidateSnapshot<TSelection> initialCandidates,
        SelectionCandidateSnapshot<TSelection> visibleCandidates,
        SelectionCandidateSnapshot<TSelection>? activeAmbiguity,
        SelectionCandidate<TSelection>? selectedCandidate,
        SelectionCandidateResolutionKind? selectedResolutionKind)
    {
        InitialCandidates = initialCandidates ?? throw new ArgumentNullException(nameof(initialCandidates));
        VisibleCandidates = visibleCandidates ?? throw new ArgumentNullException(nameof(visibleCandidates));

        if (activeAmbiguity is not null)
        {
            if (activeAmbiguity.Count < 2)
            {
                throw new ArgumentException("An active ambiguity requires at least two candidates.", nameof(activeAmbiguity));
            }

            if (activeAmbiguity.Revision != visibleCandidates.Revision)
            {
                throw new ArgumentException("Visible and ambiguous candidates must share a revision.", nameof(activeAmbiguity));
            }

            EnsureSubset(visibleCandidates, activeAmbiguity, nameof(activeAmbiguity));
        }

        if (selectedCandidate is not null)
        {
            if (!selectedResolutionKind.HasValue || !Enum.IsDefined(selectedResolutionKind.Value))
            {
                throw new ArgumentException("A selected candidate requires deterministic resolution evidence.", nameof(selectedResolutionKind));
            }

            var selectable = activeAmbiguity ?? visibleCandidates;
            if (selectedCandidate.Revision != selectable.Revision ||
                !selectable.Candidates.Any(candidate => ReferenceEquals(candidate, selectedCandidate)))
            {
                throw new ArgumentException("The selected candidate must belong to the active candidate context.", nameof(selectedCandidate));
            }
        }
        else if (selectedResolutionKind.HasValue)
        {
            throw new ArgumentException("Resolution evidence cannot exist without a selected candidate.", nameof(selectedResolutionKind));
        }

        ActiveAmbiguity = activeAmbiguity;
        SelectedCandidate = selectedCandidate;
        SelectedResolutionKind = selectedResolutionKind;
    }

    public SelectionCandidateSnapshot<TSelection> InitialCandidates { get; }

    public SelectionCandidateSnapshot<TSelection> VisibleCandidates { get; }

    public SelectionCandidateSnapshot<TSelection>? ActiveAmbiguity { get; }

    public SelectionCandidate<TSelection>? SelectedCandidate { get; }

    public SelectionCandidateResolutionKind? SelectedResolutionKind { get; }

    public SelectionCandidateSnapshot<TSelection> SelectableCandidates => ActiveAmbiguity ?? VisibleCandidates;

    public SelectionRevision Revision => VisibleCandidates.Revision;

    private static void EnsureSubset(
        SelectionCandidateSnapshot<TSelection> container,
        SelectionCandidateSnapshot<TSelection> subset,
        string parameterName)
    {
        foreach (var candidate in subset.Candidates)
        {
            if (!container.Candidates.Any(current => ReferenceEquals(current, candidate)))
            {
                throw new ArgumentException("An active candidate must come from the visible snapshot.", parameterName);
            }
        }
    }
}
