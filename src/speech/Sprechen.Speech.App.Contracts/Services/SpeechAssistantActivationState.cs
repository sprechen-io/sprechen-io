namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Beschreibt den Aktivierungszustand des Sprachassistenten für Steuerung und Rückmeldung.
/// </summary>
public enum SpeechAssistantActivationState
{
    NotStarted = 0,
    Starting = 1,
    Ready = 2,
    Paused = 3,
    Failed = 4,
}
