using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Trägt einen zu übernehmenden Feldwert und die erwartete Momentaufnahme zur stillen
/// Eingabesteuerung, damit die Übernahme an den passenden Bearbeitungsstand gebunden bleibt.
/// </summary>
internal sealed record SpeechAssistantSilentInputSubmitRequestedEvent(
    SpeechAssistantSilentInputSnapshotIdentity Expected,
    string Value) : SpeechAssistantEvent;
