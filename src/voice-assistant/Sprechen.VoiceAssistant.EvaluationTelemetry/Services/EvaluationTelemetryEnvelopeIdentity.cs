namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Verbindet Teilnehmendenkennung, Bedingung, Anwendung, Formularaufgabe und
/// Herkunft zur Identität, an die die Outbox Ereignisse und Sperren bindet.
/// </summary>
internal sealed record EvaluationTelemetryEnvelopeIdentity(
    string ParticipantId,
    string Condition,
    string ApplicationId,
    string TaskId,
    string RecordProvenance);
