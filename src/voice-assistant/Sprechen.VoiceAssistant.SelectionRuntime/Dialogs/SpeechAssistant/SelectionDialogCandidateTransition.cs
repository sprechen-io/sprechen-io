namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt einen geprüften Übergang des Kandidatenkontexts, damit die Dialogsteuerung
/// Auswahländerungen ausdrücklich an den Laufzustand übergibt.
/// </summary>
public sealed class SelectionDialogCandidateTransition<TSelection>
    where TSelection : class
{
    private SelectionDialogCandidateTransition(
        SelectionDialogCandidateTransitionKind kind,
        SelectionCandidateSnapshot<TSelection>? snapshot,
        SelectionCandidate<TSelection>? candidate,
        SelectionCandidateResolutionKind? resolutionKind)
    {
        Kind = kind;
        Snapshot = snapshot;
        Candidate = candidate;
        ResolutionKind = resolutionKind;
    }

    public SelectionDialogCandidateTransitionKind Kind { get; }

    public SelectionCandidateSnapshot<TSelection>? Snapshot { get; }

    public SelectionCandidate<TSelection>? Candidate { get; }

    public SelectionCandidateResolutionKind? ResolutionKind { get; }

    public static SelectionDialogCandidateTransition<TSelection> Preserve()
        => new(
            SelectionDialogCandidateTransitionKind.Preservation,
            snapshot: null,
            candidate: null,
            resolutionKind: null);

    public static SelectionDialogCandidateTransition<TSelection> ReplaceVisible(
        SelectionCandidateSnapshot<TSelection> snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new SelectionDialogCandidateTransition<TSelection>(
            SelectionDialogCandidateTransitionKind.VisibleReplacement,
            snapshot,
            candidate: null,
            resolutionKind: null);
    }

    public static SelectionDialogCandidateTransition<TSelection> ReplaceVisibleWithAmbiguity(
        SelectionCandidateSnapshot<TSelection> snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        EnsureAmbiguous(snapshot);
        return new SelectionDialogCandidateTransition<TSelection>(
            SelectionDialogCandidateTransitionKind.AmbiguousVisibleReplacement,
            snapshot,
            candidate: null,
            resolutionKind: null);
    }

    public static SelectionDialogCandidateTransition<TSelection> RetainAmbiguity(
        SelectionCandidateSnapshot<TSelection> snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        EnsureAmbiguous(snapshot);
        return new SelectionDialogCandidateTransition<TSelection>(
            SelectionDialogCandidateTransitionKind.AmbiguityRetention,
            snapshot,
            candidate: null,
            resolutionKind: null);
    }

    public static SelectionDialogCandidateTransition<TSelection> SelectUnique(
        SelectionCandidate<TSelection> candidate,
        SelectionCandidateResolutionKind resolutionKind)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (!Enum.IsDefined(resolutionKind))
        {
            throw new ArgumentOutOfRangeException(nameof(resolutionKind), resolutionKind, "Unknown candidate resolution kind.");
        }

        return new SelectionDialogCandidateTransition<TSelection>(
            SelectionDialogCandidateTransitionKind.UniqueSelection,
            snapshot: null,
            candidate,
            resolutionKind);
    }

    public static SelectionDialogCandidateTransition<TSelection> ResetToInitial()
        => new(
            SelectionDialogCandidateTransitionKind.InitialReset,
            snapshot: null,
            candidate: null,
            resolutionKind: null);

    public static SelectionDialogCandidateTransition<TSelection> ClearTerminal()
        => new(
            SelectionDialogCandidateTransitionKind.TerminalClearance,
            snapshot: null,
            candidate: null,
            resolutionKind: null);

    private static void EnsureAmbiguous(SelectionCandidateSnapshot<TSelection> snapshot)
    {
        if (snapshot.Count < 2)
        {
            throw new ArgumentException(
                "An ambiguity transition requires at least two retained candidates.",
                nameof(snapshot));
        }
    }
}
