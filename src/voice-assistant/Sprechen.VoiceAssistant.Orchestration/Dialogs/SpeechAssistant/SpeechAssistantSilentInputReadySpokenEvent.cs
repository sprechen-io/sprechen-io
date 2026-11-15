namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Meldet den erfolgreichen Abschluss des Wiedergabeaufrufs zur stillen Eingabe mit
/// der erwarteten Revision, damit der Reducer die zugehörige Bearbeitung freigeben kann.
/// </summary>
internal sealed record SpeechAssistantSilentInputReadySpokenEvent(long ExpectedSurfaceRevision) : SpeechAssistantEvent;
