namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet die Änderungen des beibehaltenen Kandidatenkontexts, damit die Zustandsübernahme die
/// jeweils passenden Voraussetzungen prüft.
/// </summary>
public enum SelectionDialogCandidateTransitionKind
{
    Preservation,
    VisibleReplacement,
    AmbiguousVisibleReplacement,
    AmbiguityRetention,
    UniqueSelection,
    InitialReset,
    TerminalClearance
}
