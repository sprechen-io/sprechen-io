using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Hält Bereich, Stufe und Nachricht einer späteren Assistentenspur zusammen,
/// damit die Auswahl der Diagnose von ihrem Ausgabekanal getrennt bleibt.
/// </summary>
internal readonly record struct WakeWordTelemetryTraceRecord(
    SpeechAssistantTraceScope Scope,
    SpeechAssistantTraceLevel Level,
    string Message);
