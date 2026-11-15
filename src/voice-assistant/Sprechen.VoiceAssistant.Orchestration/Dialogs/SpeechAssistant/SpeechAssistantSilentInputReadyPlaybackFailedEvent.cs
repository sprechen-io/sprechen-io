namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Meldet die fehlgeschlagene Ansage zur stillen Eingabe mit der erwarteten Revision,
/// damit der Reducer den zugehörigen Übergang zurücknehmen kann.
/// </summary>
internal sealed record SpeechAssistantSilentInputReadyPlaybackFailedEvent(long ExpectedSurfaceRevision)
    : SpeechAssistantEvent;
