using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Meldet die vom Einstellungsablauf zurückgegebene Anredeform und ermöglicht dem Reducer, den
/// Formulardialog erneut anzufordern.
/// </summary>
public sealed record SpeechAssistantVoiceCommandPolitenessChangedEvent : SpeechAssistantEvent
{
    public SpeechAssistantVoiceCommandPolitenessChangedEvent(PolitenessLevel selectedPolitenessLevel)
    {
        SelectedPolitenessLevel = selectedPolitenessLevel;
    }

    public PolitenessLevel SelectedPolitenessLevel { get; }
}
