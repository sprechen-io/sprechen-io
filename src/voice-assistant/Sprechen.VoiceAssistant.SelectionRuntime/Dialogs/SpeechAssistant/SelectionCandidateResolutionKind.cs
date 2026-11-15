namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Kennzeichnet den deterministischen Auflösungsweg eines eindeutigen Kandidaten, damit die
/// Bestätigung dessen Herkunft berücksichtigen kann.
/// </summary>
public enum SelectionCandidateResolutionKind
{
    ExactName,
    PartialName,
    Identifier,
    StableNumber
}
