namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Dienste für das Aktivierungswort und zugehöriger Laufzeitadapter des Assistenten.
/// </summary>
internal sealed class SpeechAssistantWakeWordDependencies
{
    public SpeechAssistantWakeWordDependencies(
        SpeechAssistantWakeWordInteractionDependencies interactionServices,
        SpeechAssistantWakeWordRuntime runtime)
    {
        InteractionDependencies = interactionServices ?? throw new ArgumentNullException(nameof(interactionServices));
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public SpeechAssistantWakeWordInteractionDependencies InteractionDependencies { get; }

    public SpeechAssistantWakeWordRuntime Runtime { get; }
}
