namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Überträgt Ereignisart und Daten einer Statusänderung für den gemeinsamen Versand zusammengehöriger Beobachtungen.
/// </summary>
internal sealed record EvaluationTelemetryDetailedStatusChange(string EventName, object Data);
