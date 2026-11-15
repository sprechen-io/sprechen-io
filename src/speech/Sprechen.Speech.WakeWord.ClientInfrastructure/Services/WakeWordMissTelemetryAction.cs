namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Hält ausgewählte Spur- und Logeinträge für Wake-Word-Ereignisse zusammen,
/// damit ihre Auswahl von der eigentlichen Ausgabe getrennt bleibt.
/// </summary>
internal readonly record struct WakeWordMissTelemetryAction(
    WakeWordTelemetryTraceRecord? TraceRecord,
    WakeWordTelemetryLogRecord? LogRecord)
{
    public static WakeWordMissTelemetryAction None { get; } = new(null, null);
}
