namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Überträgt Phase, Kandidatenänderung und Ansage einer formularbezogenen Intent-Behandlung. Die
/// gemeinsame Laufzeit kann den Schritt damit ohne Kenntnis des Formularmodells übernehmen.
/// </summary>
internal sealed record FormSelectionIntentHandlingResult(
    SelectionDialogPhase NextPhase,
    SelectionDialogCandidateTransition<FormInfo> CandidateTransition,
    SelectionDialogPrompt? SpokenPrompt,
    SelectionDialogCompletionReason? CompletionReason = null)
{
    public SelectionCandidate<FormInfo>? SelectedCandidate => CandidateTransition.Candidate;

    public FormInfo? Selected => SelectedCandidate?.Item;

}
