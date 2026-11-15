namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Bündelt Aktivierung, Aufnahme und akustische Rückmeldungszustände zur Darstellung der Aufnahmeoberfläche.
/// </summary>
public sealed record SpeechCaptureSurfaceStateSnapshot(
    SpeechAssistantActivationState ActivationState,
    bool IsRecording,
    EarconFamily? ActiveEarconFamily,
    SpeechWakeListeningFallbackState WakeListeningFallbackState)
{
    public SpeechCaptureSurfaceStateSnapshot(
        SpeechAssistantActivationState activationState,
        bool isRecording,
        EarconFamily? activeEarconFamily)
        : this(
            activationState,
            isRecording,
            activeEarconFamily,
            SpeechWakeListeningFallbackState.Inactive)
    {
    }
}
