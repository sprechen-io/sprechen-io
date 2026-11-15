namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Bewahrt das bestätigte Abschlussereignis samt Kennung und SHA-256-Prüfsumme, damit der lokale Abschluss genau darauf verweist.
/// </summary>
internal sealed record EvaluationTelemetryAcceptedReceipt(
    string EventId,
    string BodySha256,
    string Body);
