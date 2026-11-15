namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verwendet Formularkennung und Anzeigenamen für den gemeinsamen Kandidatenvergleich.
/// Die Formularauswahl nutzt damit dieselben Regeln für Momentaufnahmen und Treffer wie andere Auswahldialoge.
/// </summary>
internal sealed class FormSelectionMatcher : IFormSelectionMatcher
{
    public FormSelectionMatcher()
    {
        CandidateMatcher = new SelectionCandidateMatcher<FormInfo>(
            static form => form.Id,
            static form => form.DisplayName,
            TextNormalizer.NormalizeForCompactMatching);
    }

    public SelectionCandidateMatcher<FormInfo> CandidateMatcher { get; }
}
