using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

/// <summary>
/// Bestätigt in Dialogtests die Übergabe von Formularzuständen an die Oberfläche, ohne sie tatsächlich anzuzeigen.
/// Jede Sitzung erhält dabei eine neue Kennung und eine fortlaufende Nummer.
/// </summary>
internal sealed class AcceptingFormSurfaceController : ISpeechAssistantFormSurfaceController
{
    private long _generation;

    public FormSurfacePublicationSession BeginSession()
        => new(++_generation, Guid.NewGuid());

    public bool TryPublishActive(FormSurfacePublicationSession session, SpeechAssistantFormSurfaceView view)
        => true;

    public bool TryPublishTerminal(FormSurfacePublicationSession session, SpeechAssistantFormSurfaceView view)
        => true;

    public void EndSession(FormSurfacePublicationSession session)
    {
    }

    public void Stop()
    {
    }
}
