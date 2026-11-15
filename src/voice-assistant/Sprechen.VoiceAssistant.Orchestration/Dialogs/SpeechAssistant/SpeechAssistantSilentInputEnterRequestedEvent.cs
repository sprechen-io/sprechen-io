using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert den Einstieg in die stille Feldbearbeitung für eine bestimmte Momentaufnahme an, deren
/// Gültigkeit der Reducer vor der Freigabe prüft.
/// </summary>
internal sealed record SpeechAssistantSilentInputEnterRequestedEvent(
    SpeechAssistantSilentInputSnapshotIdentity Expected) : SpeechAssistantEvent;
