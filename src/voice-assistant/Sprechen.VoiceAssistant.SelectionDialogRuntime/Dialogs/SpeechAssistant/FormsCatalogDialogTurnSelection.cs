using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Leitet Ausgangsbestand, sichtbaren Bestand und ausgewählten Eintrag aus der aktuellen
/// Kandidatenmomentaufnahme ab. Zusammen mit der letzten Ansage bleiben sie dem Dialogschritt zugeordnet.
/// </summary>
internal sealed record FormsCatalogDialogTurnSelection
{
    public FormsCatalogDialogTurnSelection(
        SelectionDialogTurnSnapshot<ApplicationInfo> candidates,
        SelectionDialogPrompt? lastPrompt)
    {
        Candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        LastPrompt = lastPrompt;
    }

    public SelectionDialogTurnSnapshot<ApplicationInfo> Candidates { get; }

    public IReadOnlyList<ApplicationInfo> Applications =>
        Candidates.InitialCandidates.Candidates.Select(candidate => candidate.Item).ToArray();

    public IReadOnlyList<ApplicationInfo> VisibleApplications =>
        Candidates.VisibleCandidates.Candidates.Select(candidate => candidate.Item).ToArray();

    public ApplicationInfo? CurrentSelection => Candidates.SelectedCandidate?.Item;

    public SelectionDialogPrompt? LastPrompt { get; }
}
