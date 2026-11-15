namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt Folgephase, Kandidatenübergang und optionale Ansage eines Auswahlzugs, damit die
/// Zustandssteuerung den vorgeschlagenen Schritt prüfen kann.
/// </summary>
public sealed class SelectionDialogIntentStep<TSelection>
    where TSelection : class
{
    public SelectionDialogIntentStep(
        SelectionDialogPhase nextPhase,
        SelectionDialogCandidateTransition<TSelection> candidateTransition,
        SelectionDialogPrompt? spokenPrompt,
        SelectionDialogCompletionReason? completionReason = null)
    {
        if (!Enum.IsDefined(nextPhase))
        {
            throw new ArgumentOutOfRangeException(nameof(nextPhase), nextPhase, "Unknown selection dialog phase.");
        }

        if (completionReason.HasValue && !Enum.IsDefined(completionReason.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(completionReason),
                completionReason,
                "Unknown selection-dialog completion reason.");
        }

        NextPhase = nextPhase;
        CandidateTransition = candidateTransition ?? throw new ArgumentNullException(nameof(candidateTransition));
        SpokenPrompt = spokenPrompt;
        CompletionReason = completionReason;
    }

    public SelectionDialogPhase NextPhase { get; }

    public SelectionDialogCandidateTransition<TSelection> CandidateTransition { get; }

    public SelectionDialogPrompt? SpokenPrompt { get; }

    public SelectionDialogCompletionReason? CompletionReason { get; }

    public SelectionCandidate<TSelection>? SelectedCandidate => CandidateTransition.Candidate;

    public TSelection? Selected => SelectedCandidate?.Item;
}
