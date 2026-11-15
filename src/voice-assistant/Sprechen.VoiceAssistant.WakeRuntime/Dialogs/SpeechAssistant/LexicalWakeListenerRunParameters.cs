using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Sprache, Korrelationskennung, Aktivierungswortschatz und akustische
/// Detektorkonfiguration für einen Aufruf der Aktivierungserkennung zusammen.
/// </summary>
public sealed record LexicalWakeListenerRunParameters(
    string SelectedIsoCode,
    string CorrelationId,
    WakeWordInventory Inventory,
    AcousticWakeWordDetectorConfiguration DetectorConfiguration);
