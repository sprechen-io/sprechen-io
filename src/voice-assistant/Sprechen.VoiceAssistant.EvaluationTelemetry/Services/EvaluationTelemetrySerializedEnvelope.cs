namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Übergibt das serialisierte Ereignis samt Metadaten an die Speicherung, ohne seine Daten erneut zusammenzustellen.
/// </summary>
internal sealed record EvaluationTelemetrySerializedEnvelope(
    string EventId,
    string EventName,
    int StateVersion,
    string Body);
