namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt den nächsten Katalogschritt mit Kandidatenänderung und möglichem Neustart
/// in einer anderen Sprache. Die generische Laufzeit erhält den Übergang. Katalogbezogene
/// Rückfragen und Angaben zum Neustart bleiben gesondert verfügbar.
/// </summary>
internal sealed record FormsCatalogIntentHandlingResult(
    SelectionDialogPhase NextPhase,
    SelectionDialogCandidateTransition<ApplicationInfo> CandidateTransition,
    SelectionDialogPrompt? SpokenPrompt,
    SelectionDialogCompletionReason? CompletionReason = null,
    bool ExpectsLanguageTarget = false,
    string? RestartIsoCode = null)
{
    public SelectionCandidate<ApplicationInfo>? SelectedCandidate => CandidateTransition.Candidate;

    public ApplicationInfo? Selected => SelectedCandidate?.Item;

    /// <summary>
    /// Fordert einen Abschluss ohne eigene Ansage und mit geleertem Kandidatenzustand an. Die
    /// neue Sprache wird für den anschließenden Neustart durch den äußeren Dialog mitgegeben.
    /// </summary>
    public static FormsCatalogIntentHandlingResult SilentRestartTerminal(string restartIsoCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(restartIsoCode);
        return new FormsCatalogIntentHandlingResult(
            SelectionDialogPhase.Completed,
            SelectionDialogCandidateTransition<ApplicationInfo>.ClearTerminal(),
            SpokenPrompt: null,
            SelectionDialogCompletionReason.RestartRequested,
            RestartIsoCode: restartIsoCode);
    }
}
