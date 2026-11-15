using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert den Abbruch einer stillen Bearbeitung für die erwartete Momentaufnahme an, damit ein
/// veralteter Aufruf keinen neueren Eingabestand verändert.
/// </summary>
internal sealed record SpeechAssistantSilentInputCancelRequestedEvent(
    SpeechAssistantSilentInputSnapshotIdentity Expected) : SpeechAssistantEvent;
