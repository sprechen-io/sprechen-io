namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Bewahrt die unveränderten Versanddaten samt SHA-256-Prüfsumme und Versuchszähler für erneuten Versand und Wiederanlaufprüfung.
/// </summary>
internal sealed record EvaluationTelemetryStoredEvent(
    string EventId,
    string EventName,
    int StateVersion,
    string Body,
    string BodySha256,
    int DeliveryAttempts);
