using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Stellt die aktuelle Formularansicht samt Änderungsereignis für ihre Darstellung bereit.
/// </summary>
public interface ISpeechAssistantFormSurfaceState
{
    SpeechAssistantFormSurfaceView? Current { get; }

    event Action? Changed;
}
