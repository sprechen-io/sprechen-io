namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Steuert Veröffentlichung und Ende von Formularansichten innerhalb einer zugeordneten Sitzung.
/// </summary>
public interface ISpeechAssistantFormSurfaceController
{
    FormSurfacePublicationSession BeginSession();

    bool TryPublishActive(FormSurfacePublicationSession session, SpeechAssistantFormSurfaceView view);

    bool TryPublishTerminal(FormSurfacePublicationSession session, SpeechAssistantFormSurfaceView view);

    void EndSession(FormSurfacePublicationSession session);

    void Stop();
}
