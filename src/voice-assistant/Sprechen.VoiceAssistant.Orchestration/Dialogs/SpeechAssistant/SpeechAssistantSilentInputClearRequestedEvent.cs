using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert das Leeren des Feldwerts für die erwartete Momentaufnahme an und unterscheidet diesen
/// Auftrag vom Abbruch der Bearbeitung.
/// </summary>
internal sealed record SpeechAssistantSilentInputClearRequestedEvent(
    SpeechAssistantSilentInputSnapshotIdentity Expected) : SpeechAssistantEvent;
