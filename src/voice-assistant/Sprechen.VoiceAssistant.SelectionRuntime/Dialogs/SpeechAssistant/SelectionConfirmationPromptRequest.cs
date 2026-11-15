namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Übergibt Kandidat, Ausgangsbestand und Bestätigungsstufe an die Ansagefunktion.
/// Beim Erzeugen wird die Identität des Kandidaten anhand des Bestands geprüft.
/// Übereinstimmende Revisionen der Kandidateninstanzen sind nicht erforderlich.
/// </summary>
public sealed class SelectionConfirmationPromptRequest<TSelection>
    where TSelection : class
{
    internal SelectionConfirmationPromptRequest(
        SelectionConfirmationPromptKind kind,
        SelectionCandidate<TSelection> candidate,
        SelectionCandidateSnapshot<TSelection> retainedCandidates,
        SelectionConfirmationCaptureStage stage,
        SelectionDialogPrompt? activeStagePrompt)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown confirmation prompt kind.");
        }

        if (!Enum.IsDefined(stage))
        {
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown confirmation capture stage.");
        }

        Kind = kind;
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        RetainedCandidates = retainedCandidates ?? throw new ArgumentNullException(nameof(retainedCandidates));
        Stage = stage;
        ActiveStagePrompt = activeStagePrompt;

        var retainedCandidate = RetainedCandidates.Candidates.FirstOrDefault(
            current => current.StableNumber == Candidate.StableNumber);
        if (retainedCandidate is null ||
            !string.Equals(retainedCandidate.Identifier, Candidate.Identifier, StringComparison.Ordinal) ||
            !string.Equals(retainedCandidate.DisplayName, Candidate.DisplayName, StringComparison.Ordinal) ||
            !ReferenceEquals(retainedCandidate.Item, Candidate.Item))
        {
            throw new ArgumentException(
                "The retained candidate snapshot must contain the selected candidate identity.",
                nameof(retainedCandidates));
        }

        if (kind is SelectionConfirmationPromptKind.ActiveStage or SelectionConfirmationPromptKind.ActiveStageList &&
            (activeStagePrompt is null || !activeStagePrompt.HasContent))
        {
            throw new ArgumentException(
                "An active-stage prompt request requires the current modeled stage prompt.",
                nameof(activeStagePrompt));
        }
    }

    public SelectionConfirmationPromptKind Kind { get; }

    public SelectionCandidate<TSelection> Candidate { get; }

    /// <summary>
    /// Stellt den unveränderlichen Ausgangsbestand in seiner übernommenen Reihenfolge bereit.
    /// </summary>
    public SelectionCandidateSnapshot<TSelection> RetainedCandidates { get; }

    /// <summary>
    /// Stellt die kandidatenbezogene Bestätigungsstufe bereit.
    /// </summary>
    public SelectionConfirmationCaptureStage Stage { get; }

    /// <summary>
    /// Stellt die aktive Stufenansage zum Wiederholen oder Zusammensetzen bereit.
    /// </summary>
    public SelectionDialogPrompt? ActiveStagePrompt { get; }
}
