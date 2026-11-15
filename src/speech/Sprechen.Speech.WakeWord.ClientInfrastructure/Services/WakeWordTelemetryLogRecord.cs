namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Hält Nachricht und optionale Ausnahme für einen späteren Logeintrag zusammen,
/// damit die Auswahl der Diagnose keine direkte Loggerausgabe benötigt.
/// </summary>
internal readonly record struct WakeWordTelemetryLogRecord(
    Exception? Exception,
    string Message);
