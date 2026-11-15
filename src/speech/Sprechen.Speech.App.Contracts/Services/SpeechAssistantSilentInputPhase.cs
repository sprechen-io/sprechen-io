namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Beschreibt die Phase der stillen Eingabe für die Koordination von Bearbeitung und Rückmeldung.
/// </summary>
public enum SpeechAssistantSilentInputPhase
{
    Unavailable = 0,
    Available = 1,
    Transitioning = 2,
    Editing = 3,
    Pending = 4,
    Repair = 5,
}
