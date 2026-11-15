using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verwaltet Phase, Kandidaten, Zähler und ausstehende Ansagen eines Auswahllaufs. Momentaufnahmen
/// geben Entscheidungen den benötigten Kontext, ohne den gesamten Zustand freizugeben.
/// </summary>
internal sealed partial class SelectionDialogRunState<TSelection>
    where TSelection : class
{
    public SelectionDialogRunState(
        SelectionCandidateSnapshot<TSelection> initialCandidates,
        SpeechTranscript? initialListeningTranscript)
    {
        InitialCandidates = initialCandidates ?? throw new ArgumentNullException(nameof(initialCandidates));
        VisibleCandidates = initialCandidates;
        InitialListeningTranscript = initialListeningTranscript;
        Phase = initialListeningTranscript is null
            ? SelectionDialogPhase.Announcing
            : SelectionDialogPhase.Listening;
    }

    public SelectionDialogPhase Phase { get; private set; }

    public SelectionCandidateSnapshot<TSelection> InitialCandidates { get; }

    public SelectionCandidateSnapshot<TSelection> VisibleCandidates { get; private set; }

    public SelectionCandidateSnapshot<TSelection>? ActiveAmbiguity { get; private set; }

    public SelectionCandidate<TSelection>? SelectedCandidate { get; private set; }

    public SelectionCandidateResolutionKind? SelectedResolutionKind { get; private set; }

    public SelectionDialogPrompt? LastPrompt { get; set; }

    public SelectionDialogPrompt? PendingPrompt { get; set; }

    public SelectionDialogCompletionReason? CompletionReason { get; private set; }

    public int NoInputCount { get; private set; }

    public int RecoverableFailureCount { get; private set; }

    public SelectionConfirmationCaptureStage? ConfirmationCaptureStage { get; private set; }

    public int ConfirmationCaptureCount { get; private set; }

    public int ConfirmationRepairFailureCount { get; private set; }

    public SelectionDialogPrompt? ActiveConfirmationPrompt { get; private set; }

    public SpeechTranscript? InitialListeningTranscript { get; private set; }

    public SpeechCaptureResult? PrefetchedCaptureResult { get; set; }

    public bool UsedInitialListeningTranscript { get; private set; }

    public bool IsTerminal => Phase is SelectionDialogPhase.Completed or SelectionDialogPhase.Launch;

    public void CompleteAnnouncement()
    {
        if (Phase != SelectionDialogPhase.Announcing)
        {
            throw new SelectionCandidateStateException(
                "Only an announcing selection dialog can enter listening.");
        }

        Phase = SelectionDialogPhase.Listening;
    }

    public void ReturnToAnnouncement()
    {
        if (Phase != SelectionDialogPhase.Listening)
        {
            throw new SelectionCandidateStateException(
                "Only a listening selection dialog can return to announcing.");
        }

        Phase = SelectionDialogPhase.Announcing;
    }

    public SelectionDialogTurnSnapshot<TSelection> CreateTurnSnapshot()
        => new(
            InitialCandidates,
            VisibleCandidates,
            ActiveAmbiguity,
            SelectedCandidate,
            SelectedResolutionKind);

    public SelectionConfirmationStateSnapshot<TSelection> CreateConfirmationStateSnapshot()
        => new(
            Phase,
            CreateTurnSnapshot(),
            ConfirmationCaptureStage,
            ConfirmationCaptureCount,
            ConfirmationRepairFailureCount,
            RecoverableFailureCount,
            ActiveConfirmationPrompt);

    public void RegisterNoInput()
    {
        NoInputCount = checked(NoInputCount + 1);
    }

    public void RegisterUsableTranscript()
    {
        NoInputCount = 0;
        RecoverableFailureCount = 0;
    }

    public void ResetTurnDiagnostics()
    {
        NoInputCount = 0;
        RecoverableFailureCount = 0;
    }

    public void BeginTurn()
    {
        PrefetchedCaptureResult = CreateInitialCaptureResult(InitialListeningTranscript);
        UsedInitialListeningTranscript = PrefetchedCaptureResult?.Transcript is not null;
        InitialListeningTranscript = null;
    }

    private static SpeechCaptureResult? CreateInitialCaptureResult(SpeechTranscript? initialListeningTranscript)
        => initialListeningTranscript is null
            ? null
            : new SpeechCaptureResult(
                initialListeningTranscript.Text,
                initialListeningTranscript.ConfidencePercent);
}
